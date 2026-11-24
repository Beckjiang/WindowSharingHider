using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using WindowSharingShower;

static class CaptureSmokeTests
{
    [DllImport("user32.dll", SetLastError = true)] static extern bool SetWindowDisplayAffinity(IntPtr window, uint affinity);
    [DllImport("user32.dll", SetLastError = true)] static extern bool PostMessage(IntPtr window, uint message, IntPtr w, IntPtr l);
    [DllImport("user32.dll", SetLastError = true)] static extern bool SendMessageTimeout(IntPtr window, uint message, IntPtr w, IntPtr l, uint flags, uint timeout, out UIntPtr result);
    const uint ResetProtection = 0x8001;

    sealed class Fixture : Form
    {
        public Fixture()
        {
            Text = "Capture smoke fixture " + Process.GetCurrentProcess().Id;
            Width = 360; Height = 120;
            Controls.Add(new Label { Dock = DockStyle.Fill, Text = "Disposable capture protection fixture", TextAlign = System.Drawing.ContentAlignment.MiddleCenter });
            Shown += (s, e) => Protect();
        }
        void Protect()
        {
            if (!SetWindowDisplayAffinity(Handle, 0x11))
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }
        protected override void WndProc(ref Message message)
        {
            if (message.Msg == ResetProtection) { Protect(); return; }
            base.WndProc(ref message);
        }
    }

    [STAThread]
    static int Main(string[] args)
    {
        if (args.Length == 1 && args[0] == "--fixture")
        {
            Application.Run(new Fixture());
            return 0;
        }
        try
        {
            Run("x64");
            Run("x86");
            uint affinity; string error;
            if (WindowHandler.TryGetWindowDisplayAffinity(IntPtr.Zero, out affinity, out error) || string.IsNullOrEmpty(error))
                throw new Exception("Invalid HWND was reported as visible.");
            try { WindowHandler.ShowInCapture(IntPtr.Zero); throw new Exception("Invalid HWND was accepted."); }
            catch (System.ComponentModel.Win32Exception) { }
            Console.WriteLine("PASS invalid HWND reports a Win32 error");
            Console.WriteLine("ALL TESTS PASSED");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    static void Run(string architecture)
    {
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CaptureSmokeTests." + architecture + ".exe");
        var start = new ProcessStartInfo(path, "--fixture") { UseShellExecute = false };
        using (var process = Process.Start(start))
        {
            IntPtr window = IntPtr.Zero;
            try
            {
                var deadline = DateTime.UtcNow.AddSeconds(10);
                while (DateTime.UtcNow < deadline)
                {
                    process.Refresh();
                    if (process.HasExited) throw new Exception(architecture + " fixture exited early.");
                    window = process.MainWindowHandle;
                    if (window != IntPtr.Zero && WindowHandler.GetWindowDisplayAffinity(window) == 0x11) break;
                    Thread.Sleep(50);
                }
                if (window == IntPtr.Zero || WindowHandler.GetWindowDisplayAffinity(window) != 0x11)
                    throw new Exception(architecture + " fixture did not become capture-excluded.");
                int handlesBefore = Process.GetCurrentProcess().HandleCount;
                for (int i = 0; i < 10; i++)
                {
                    if (i != 0)
                    {
                        UIntPtr ignored;
                        if (!SendMessageTimeout(window, ResetProtection, IntPtr.Zero, IntPtr.Zero, 2, 2000, out ignored))
                            throw new Exception("Unable to reset fixture protection.");
                    }
                    if (WindowHandler.GetWindowDisplayAffinity(window) != 0x11) throw new Exception("Fixture was not protected.");
                    WindowHandler.ShowInCapture(window);
                    if (WindowHandler.GetWindowDisplayAffinity(window) != 0) throw new Exception("Protection was not cleared.");
                    WindowHandler.ShowInCapture(window); // Already visible: no remote thread required.
                }
                int handlesAfter = Process.GetCurrentProcess().HandleCount;
                if (handlesAfter - handlesBefore > 4) throw new Exception("Possible process/thread handle leak.");
                Console.WriteLine("PASS " + architecture + ": 10 remote calls, affinity 0x11 -> 0x0, visible no-op, handle delta " + (handlesAfter - handlesBefore));
            }
            finally
            {
                if (!process.HasExited)
                {
                    if (window != IntPtr.Zero) PostMessage(window, 0x10, IntPtr.Zero, IntPtr.Zero);
                    if (!process.WaitForExit(3000)) { process.Kill(); process.WaitForExit(); }
                }
            }
        }
    }
}

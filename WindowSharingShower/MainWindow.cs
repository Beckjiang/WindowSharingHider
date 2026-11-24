using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowSharingShower
{
    public partial class MainWindow : Form
    {
        public class WindowInfo
        {
            public string Title { get; set; }
            public IntPtr Handle { get; set; }
            public bool stillExists;
            public bool? HiddenFromCapture { get; set; }
            public bool DisplayChanged { get; set; }
            public bool Applying;
            public bool StopRetry;
            public DateTime RetryAfter;
            public string Error;

            public override string ToString()
            {
                string state = HiddenFromCapture == null ? "状态未知" : HiddenFromCapture.Value ? "共享中隐藏" : "共享中可见";
                return Title + "  [" + state + "]" + (Applying ? "  正在处理…" : Error == null ? "" : "  [" + Error + "]");
            }
        }

        readonly Timer refreshTimer;
        readonly ToolStripStatusLabel statusText;
        readonly HashSet<IntPtr> initialWindows;
        bool syncingUi;
        public static string DiagnosticPath { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "WindowSharingShower", "diagnostics.log"); } }

        public MainWindow() : this(new IntPtr[0]) { }

        public MainWindow(IEnumerable<IntPtr> windows)
        {
            initialWindows = new HashSet<IntPtr>(windows);
            InitializeComponent();
            bool administrator;
            using (var identity = WindowsIdentity.GetCurrent())
                administrator = new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
            Text += administrator ? "（管理员）" : "（未提权）";
            ClientSize = new Size(760, 520);
            windowListCheckBox.HorizontalScrollbar = true;
            statusText = new ToolStripStatusLabel { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
            var statusStrip = new StatusStrip { ShowItemToolTips = true };
            statusStrip.Items.Add(statusText);
            Controls.Add(statusStrip);
            statusStrip.BringToFront();
            SetStatus(administrator ? "已以管理员身份运行，请勾选目标窗口。" : "当前未提权，访问管理员程序可能失败。");
            refreshTimer = new Timer { Interval = 500 };
            refreshTimer.Tick += Timer_Tick;
            Shown += (s, e) => { Timer_Tick(s, e); refreshTimer.Start(); };
            FormClosed += (s, e) => refreshTimer.Dispose();
        }

        void SetStatus(string text)
        {
            statusText.Text = text;
            statusText.ToolTipText = text;
        }

        static void Log(string message)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(DiagnosticPath));
                File.AppendAllText(DiagnosticPath, DateTimeOffset.Now.ToString("O") + " " + message + Environment.NewLine);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }

        void windowListCheckBox_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (syncingUi) return;
            var window = (WindowInfo)windowListCheckBox.Items[e.Index];
            window.Error = null;
            window.StopRetry = false;
            window.RetryAfter = DateTime.MinValue;
            window.DisplayChanged = true;
            if (e.NewValue == CheckState.Checked) Apply(window);
        }

        async void Apply(WindowInfo window)
        {
            if (window.Applying || window.StopRetry || DateTime.UtcNow < window.RetryAfter) return;
            window.Applying = true;
            window.DisplayChanged = true;
            try
            {
                await Task.Run(() => WindowHandler.ShowInCapture(window.Handle));
                if (IsDisposed) return;
                Log("HWND=0x" + window.Handle.ToInt64().ToString("X") + " " + window.Title + " 设置成功，affinity=0x0");
                window.Error = null;
                window.HiddenFromCapture = false;
                SetStatus(window.Title + "：已设为共享中可见。");
            }
            catch (Exception error)
            {
                if (IsDisposed) return;
                if (window.Error != error.Message)
                    Log("HWND=0x" + window.Handle.ToInt64().ToString("X") + " " + window.Title + " " + error);
                window.Error = error.Message;
                window.RetryAfter = DateTime.UtcNow.AddSeconds(5);
                window.StopRetry = error is TimeoutException;
                SetStatus(window.Title + "：" + error.Message);
            }
            finally
            {
                window.Applying = false;
                window.DisplayChanged = true;
            }
        }

        void ReadState(WindowInfo window)
        {
            uint affinity; string error;
            bool? hidden = WindowHandler.TryGetWindowDisplayAffinity(window.Handle, out affinity, out error) ? (bool?)(affinity != 0) : null;
            if (window.HiddenFromCapture != hidden) { window.HiddenFromCapture = hidden; window.DisplayChanged = true; }
            if (hidden == null && !window.Applying && window.Error != error)
            {
                window.Error = error;
                window.DisplayChanged = true;
            }
        }

        void Timer_Tick(object sender, EventArgs e)
        {
            syncingUi = true;
            windowListCheckBox.BeginUpdate();
            try
            {
                foreach (WindowInfo window in windowListCheckBox.Items) window.stillExists = false;
                foreach (var pair in WindowHandler.GetVisibleWindows())
                {
                    var window = windowListCheckBox.Items.Cast<WindowInfo>().FirstOrDefault(w => w.Handle == pair.Key);
                    if (window == null)
                    {
                        window = new WindowInfo { Title = pair.Value, Handle = pair.Key, DisplayChanged = true };
                        ReadState(window);
                        int index = windowListCheckBox.Items.Add(window);
                        if (initialWindows.Remove(pair.Key)) windowListCheckBox.SetItemChecked(index, true);
                    }
                    if (window.Title != pair.Value) { window.Title = pair.Value; window.DisplayChanged = true; }
                    window.stillExists = true;
                }
                foreach (var window in windowListCheckBox.Items.Cast<WindowInfo>().Where(w => !w.stillExists).ToArray())
                    windowListCheckBox.Items.Remove(window);
                for (int i = 0; i < windowListCheckBox.Items.Count; i++)
                {
                    var window = (WindowInfo)windowListCheckBox.Items[i];
                    ReadState(window);
                    if (windowListCheckBox.GetItemChecked(i) && window.HiddenFromCapture == true) Apply(window);
                    if (window.DisplayChanged) { RefreshItem(i); window.DisplayChanged = false; }
                }
            }
            catch (Exception error) { SetStatus(error.Message); }
            finally { windowListCheckBox.EndUpdate(); syncingUi = false; }
        }

        void RefreshItem(int index)
        {
            var item = windowListCheckBox.Items[index];
            bool selected = windowListCheckBox.GetItemChecked(index);
            windowListCheckBox.Items[index] = item;
            windowListCheckBox.SetItemChecked(index, selected);
        }
    }
}

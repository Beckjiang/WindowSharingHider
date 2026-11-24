using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;

namespace WindowSharingShower
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var windows = new List<IntPtr>();
            for (int i = 0; i < args.Length; i++)
            {
                long handle;
                if (args[i] != "--show-window" || ++i == args.Length ||
                    !long.TryParse(args[i].StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? args[i].Substring(2) : args[i],
                        NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out handle) || handle <= 0)
                {
                    MessageBox.Show("用法：WindowSharingShower.exe [--show-window 0x窗口句柄]；默认仅处理手动勾选的窗口。", "启动参数无效");
                    return;
                }
                windows.Add(new IntPtr(handle));
            }
            Application.Run(new MainWindow(windows));
        }
    }
}

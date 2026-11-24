using System;
using System.Linq;
using System.Windows.Forms;

namespace WindowSharingHider
{
    public partial class MainWindow : Form
    {
        public class WindowInfo
        {
            public String Title { get; set; }
            public IntPtr Handle { get; set; }
            public Boolean stillExists = false;
            public Boolean HiddenFromCapture { get; set; }
            public Boolean DisplayChanged { get; set; }

            public override string ToString()
            {
                var state = HiddenFromCapture ? "共享中隐藏" : "共享中可见";
                return Title + "  [" + state + "]";
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            UpdateHint();
            var timer = new Timer();
            timer.Interval = 500;
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        Boolean preserveSettings = false;
        Boolean syncingUi = false;

        Boolean IsShowMode
        {
            get { return rbShowMode.Checked; }
        }

        void UpdateHint()
        {
            lblHint.Text = IsShowMode
                ? "勾选的窗口会强制出现在屏幕共享中，包括已被隐藏的窗口"
                : "勾选的窗口不会出现在屏幕共享中";
        }

        void Mode_CheckedChanged(object sender, EventArgs e)
        {
            var radio = sender as RadioButton;
            if (radio == null || !radio.Checked) return;
            UpdateHint();
            if (!preserveSettings) return;
            ApplyAll();
        }

        void windowListCheckBox_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (syncingUi || !preserveSettings) return;
            var window = (WindowInfo)windowListCheckBox.Items[e.Index];
            ApplyAffinity(window, e.NewValue == CheckState.Checked);
        }

        void ApplyAll()
        {
            for (var i = 0; i < windowListCheckBox.Items.Count; i++)
            {
                var window = (WindowInfo)windowListCheckBox.Items[i];
                ApplyAffinity(window, windowListCheckBox.GetItemChecked(i));
            }
        }

        void ApplyAffinity(WindowInfo window, Boolean isChecked)
        {
            var actuallyHidden = WindowHandler.IsHiddenFromCapture(window.Handle);
            Boolean shouldHide;
            if (IsShowMode)
            {
                if (!isChecked) return;
                shouldHide = false;
            }
            else
            {
                shouldHide = isChecked;
            }

            if (shouldHide == actuallyHidden) return;
            WindowHandler.SetWindowDisplayAffinity(
                window.Handle,
                shouldHide ? WindowHandler.WdaExcludeFromCapture : WindowHandler.WdaNone);
        }

        void Timer_Tick(object sender, EventArgs e)
        {
            syncingUi = true;
            windowListCheckBox.BeginUpdate();
            try
            {
                foreach (WindowInfo window in windowListCheckBox.Items) window.stillExists = false;
                var currWindows = WindowHandler.GetVisibleWindows();
                foreach (var window in currWindows)
                {
                    var existingWindow = windowListCheckBox.Items.Cast<WindowInfo>().FirstOrDefault(i => i.Handle == window.Key);
                    if (existingWindow != null)
                    {
                        existingWindow.stillExists = true;
                        if (existingWindow.Title != window.Value)
                        {
                            existingWindow.Title = window.Value;
                            existingWindow.DisplayChanged = true;
                        }
                    }
                    else windowListCheckBox.Items.Add(new WindowInfo { Title = window.Value, Handle = window.Key, stillExists = true, DisplayChanged = true });
                }
                foreach (var window in windowListCheckBox.Items.Cast<WindowInfo>().ToArray()) if (window.stillExists == false) windowListCheckBox.Items.Remove(window);
                for (var i = 0; i < windowListCheckBox.Items.Count; i++)
                {
                    var window = (WindowInfo)windowListCheckBox.Items[i];
                    if (preserveSettings)
                    {
                        ApplyAffinity(window, windowListCheckBox.GetItemChecked(i));
                    }
                    else
                    {
                        windowListCheckBox.SetItemChecked(i, WindowHandler.IsHiddenFromCapture(window.Handle));
                    }

                    var actuallyHidden = WindowHandler.IsHiddenFromCapture(window.Handle);
                    if (window.HiddenFromCapture != actuallyHidden)
                    {
                        window.HiddenFromCapture = actuallyHidden;
                        window.DisplayChanged = true;
                    }
                    if (window.DisplayChanged)
                    {
                        RefreshItem(i);
                        window.DisplayChanged = false;
                    }
                }
            }
            finally
            {
                windowListCheckBox.EndUpdate();
                syncingUi = false;
                preserveSettings = true;
            }
        }

        void RefreshItem(Int32 index)
        {
            var item = windowListCheckBox.Items[index];
            var isChecked = windowListCheckBox.GetItemChecked(index);
            windowListCheckBox.Items[index] = item;
            windowListCheckBox.SetItemChecked(index, isChecked);
        }
    }
}

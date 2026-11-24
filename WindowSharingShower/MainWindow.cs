using System;
using System.Linq;
using System.Windows.Forms;

namespace WindowSharingShower
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
            var timer = new Timer();
            timer.Interval = 500;
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        Boolean syncingUi = false;

        void windowListCheckBox_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (syncingUi) return;
            if (e.NewValue != CheckState.Checked) return;
            var window = (WindowInfo)windowListCheckBox.Items[e.Index];
            WindowHandler.ShowInCapture(window.Handle);
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
                    else
                    {
                        windowListCheckBox.Items.Add(new WindowInfo
                        {
                            Title = window.Value,
                            Handle = window.Key,
                            stillExists = true,
                            HiddenFromCapture = WindowHandler.IsHiddenFromCapture(window.Key),
                            DisplayChanged = true
                        });
                    }
                }
                foreach (var window in windowListCheckBox.Items.Cast<WindowInfo>().ToArray()) if (window.stillExists == false) windowListCheckBox.Items.Remove(window);
                for (var i = 0; i < windowListCheckBox.Items.Count; i++)
                {
                    var window = (WindowInfo)windowListCheckBox.Items[i];
                    if (windowListCheckBox.GetItemChecked(i))
                        WindowHandler.ShowInCapture(window.Handle);

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

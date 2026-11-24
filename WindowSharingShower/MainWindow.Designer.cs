namespace WindowSharingShower
{
    partial class MainWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.panelHint = new System.Windows.Forms.Panel();
            this.lblHint = new System.Windows.Forms.Label();
            this.windowListCheckBox = new System.Windows.Forms.CheckedListBox();
            this.panelHint.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelHint
            // 
            this.panelHint.Controls.Add(this.lblHint);
            this.panelHint.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHint.Location = new System.Drawing.Point(0, 0);
            this.panelHint.Name = "panelHint";
            this.panelHint.Size = new System.Drawing.Size(360, 40);
            this.panelHint.TabIndex = 0;
            // 
            // lblHint
            // 
            this.lblHint.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblHint.Location = new System.Drawing.Point(12, 8);
            this.lblHint.Name = "lblHint";
            this.lblHint.Size = new System.Drawing.Size(336, 24);
            this.lblHint.TabIndex = 0;
            this.lblHint.Text = "勾选窗口，将其强制显示在屏幕共享中";
            this.lblHint.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // windowListCheckBox
            // 
            this.windowListCheckBox.CheckOnClick = true;
            this.windowListCheckBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.windowListCheckBox.FormattingEnabled = true;
            this.windowListCheckBox.IntegralHeight = false;
            this.windowListCheckBox.Location = new System.Drawing.Point(0, 40);
            this.windowListCheckBox.Name = "windowListCheckBox";
            this.windowListCheckBox.Size = new System.Drawing.Size(360, 380);
            this.windowListCheckBox.TabIndex = 1;
            this.windowListCheckBox.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.windowListCheckBox_ItemCheck);
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(360, 420);
            this.Controls.Add(this.windowListCheckBox);
            this.Controls.Add(this.panelHint);
            this.MinimumSize = new System.Drawing.Size(300, 280);
            this.Name = "MainWindow";
            this.Text = "Window Sharing Shower";
            this.panelHint.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelHint;
        private System.Windows.Forms.Label lblHint;
        private System.Windows.Forms.CheckedListBox windowListCheckBox;
    }
}

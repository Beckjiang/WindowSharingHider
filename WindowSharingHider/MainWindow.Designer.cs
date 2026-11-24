namespace WindowSharingHider
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
            this.panelMode = new System.Windows.Forms.Panel();
            this.lblHint = new System.Windows.Forms.Label();
            this.rbShowMode = new System.Windows.Forms.RadioButton();
            this.rbHideMode = new System.Windows.Forms.RadioButton();
            this.windowListCheckBox = new System.Windows.Forms.CheckedListBox();
            this.panelMode.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelMode
            // 
            this.panelMode.Controls.Add(this.lblHint);
            this.panelMode.Controls.Add(this.rbShowMode);
            this.panelMode.Controls.Add(this.rbHideMode);
            this.panelMode.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelMode.Location = new System.Drawing.Point(0, 0);
            this.panelMode.Name = "panelMode";
            this.panelMode.Size = new System.Drawing.Size(360, 64);
            this.panelMode.TabIndex = 0;
            // 
            // lblHint
            // 
            this.lblHint.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblHint.Location = new System.Drawing.Point(12, 32);
            this.lblHint.Name = "lblHint";
            this.lblHint.Size = new System.Drawing.Size(336, 28);
            this.lblHint.TabIndex = 2;
            this.lblHint.Text = "勾选的窗口不会出现在屏幕共享中";
            // 
            // rbShowMode
            // 
            this.rbShowMode.AutoSize = true;
            this.rbShowMode.Location = new System.Drawing.Point(100, 8);
            this.rbShowMode.Name = "rbShowMode";
            this.rbShowMode.Size = new System.Drawing.Size(71, 17);
            this.rbShowMode.TabIndex = 1;
            this.rbShowMode.Text = "显示模式";
            this.rbShowMode.UseVisualStyleBackColor = true;
            this.rbShowMode.CheckedChanged += new System.EventHandler(this.Mode_CheckedChanged);
            // 
            // rbHideMode
            // 
            this.rbHideMode.AutoSize = true;
            this.rbHideMode.Checked = true;
            this.rbHideMode.Location = new System.Drawing.Point(12, 8);
            this.rbHideMode.Name = "rbHideMode";
            this.rbHideMode.Size = new System.Drawing.Size(71, 17);
            this.rbHideMode.TabIndex = 0;
            this.rbHideMode.TabStop = true;
            this.rbHideMode.Text = "隐藏模式";
            this.rbHideMode.UseVisualStyleBackColor = true;
            this.rbHideMode.CheckedChanged += new System.EventHandler(this.Mode_CheckedChanged);
            // 
            // windowListCheckBox
            // 
            this.windowListCheckBox.CheckOnClick = true;
            this.windowListCheckBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.windowListCheckBox.FormattingEnabled = true;
            this.windowListCheckBox.IntegralHeight = false;
            this.windowListCheckBox.Location = new System.Drawing.Point(0, 64);
            this.windowListCheckBox.Name = "windowListCheckBox";
            this.windowListCheckBox.Size = new System.Drawing.Size(360, 356);
            this.windowListCheckBox.TabIndex = 1;
            this.windowListCheckBox.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.windowListCheckBox_ItemCheck);
            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(360, 420);
            this.Controls.Add(this.windowListCheckBox);
            this.Controls.Add(this.panelMode);
            this.MinimumSize = new System.Drawing.Size(300, 280);
            this.Name = "MainWindow";
            this.Text = "Window Sharing Hider";
            this.panelMode.ResumeLayout(false);
            this.panelMode.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelMode;
        private System.Windows.Forms.Label lblHint;
        private System.Windows.Forms.RadioButton rbShowMode;
        private System.Windows.Forms.RadioButton rbHideMode;
        private System.Windows.Forms.CheckedListBox windowListCheckBox;
    }
}

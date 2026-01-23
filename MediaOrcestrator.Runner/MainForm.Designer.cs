namespace MediaOrcestrator.Runner
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            uiMediaSourcePanel = new Panel();
            btnSync = new Button();
            mediaMatrixGridControl1 = new MediaMatrixGridControl();
            SuspendLayout();
            // 
            // uiMediaSourcePanel
            // 
            uiMediaSourcePanel.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            uiMediaSourcePanel.BackColor = SystemColors.ControlDark;
            uiMediaSourcePanel.Location = new Point(12, 50);
            uiMediaSourcePanel.Name = "uiMediaSourcePanel";
            uiMediaSourcePanel.Size = new Size(231, 394);
            uiMediaSourcePanel.TabIndex = 0;
            // 
            // btnSync
            // 
            btnSync.Location = new Point(12, 12);
            btnSync.Name = "btnSync";
            btnSync.Size = new Size(231, 32);
            btnSync.TabIndex = 1;
            btnSync.Text = "Синхронизировать";
            btnSync.UseVisualStyleBackColor = true;
            btnSync.Click += btnSync_Click;
            // 
            // mediaMatrixGridControl1
            // 
            mediaMatrixGridControl1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            mediaMatrixGridControl1.Location = new Point(249, 12);
            mediaMatrixGridControl1.Name = "mediaMatrixGridControl1";
            mediaMatrixGridControl1.Size = new Size(539, 432);
            mediaMatrixGridControl1.TabIndex = 2;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 456);
            Controls.Add(mediaMatrixGridControl1);
            Controls.Add(btnSync);
            Controls.Add(uiMediaSourcePanel);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "MainForm";
            Text = "Медиа оркестратор";
            Load += MainForm_Load;
            ResumeLayout(false);
        }

        #endregion

        private Panel uiMediaSourcePanel;
        private Button btnSync;
        private MediaMatrixGridControl mediaMatrixGridControl1;
    }
}

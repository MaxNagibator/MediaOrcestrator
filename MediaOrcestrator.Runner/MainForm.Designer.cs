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
            uiAddSourceButton = new Button();
            uiSourcesComboBox = new ComboBox();
            splitContainer1 = new SplitContainer();
            panel1 = new Panel();
            button1 = new Button();
            label2 = new Label();
            label1 = new Label();
            uiRelationToComboBox = new ComboBox();
            uiRelationFromComboBox = new ComboBox();
            ((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
            splitContainer1.Panel1.SuspendLayout();
            splitContainer1.Panel2.SuspendLayout();
            splitContainer1.SuspendLayout();
            SuspendLayout();
            // 
            // uiMediaSourcePanel
            // 
            uiMediaSourcePanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            uiMediaSourcePanel.AutoScroll = true;
            uiMediaSourcePanel.BackColor = SystemColors.ControlDark;
            uiMediaSourcePanel.Location = new Point(3, 43);
            uiMediaSourcePanel.Name = "uiMediaSourcePanel";
            uiMediaSourcePanel.Padding = new Padding(5);
            uiMediaSourcePanel.Size = new Size(396, 336);
            uiMediaSourcePanel.TabIndex = 0;
            uiMediaSourcePanel.SizeChanged += uiMediaSourcePanel_SizeChanged;
            // 
            // btnSync
            // 
            btnSync.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            btnSync.Location = new Point(3, 5);
            btnSync.Name = "btnSync";
            btnSync.Size = new Size(396, 32);
            btnSync.TabIndex = 1;
            btnSync.Text = "Синхронизировать";
            btnSync.UseVisualStyleBackColor = true;
            btnSync.Click += btnSync_Click;
            // 
            // mediaMatrixGridControl1
            // 
            mediaMatrixGridControl1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            mediaMatrixGridControl1.Location = new Point(3, 3);
            mediaMatrixGridControl1.Name = "mediaMatrixGridControl1";
            mediaMatrixGridControl1.Size = new Size(721, 764);
            mediaMatrixGridControl1.TabIndex = 2;
            // 
            // uiAddSourceButton
            // 
            uiAddSourceButton.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            uiAddSourceButton.Location = new Point(3, 414);
            uiAddSourceButton.Name = "uiAddSourceButton";
            uiAddSourceButton.Size = new Size(396, 23);
            uiAddSourceButton.TabIndex = 3;
            uiAddSourceButton.Text = "Добавить источник";
            uiAddSourceButton.UseVisualStyleBackColor = true;
            uiAddSourceButton.Click += uiAddSourceButton_Click;
            // 
            // uiSourcesComboBox
            // 
            uiSourcesComboBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            uiSourcesComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            uiSourcesComboBox.FormattingEnabled = true;
            uiSourcesComboBox.Location = new Point(3, 385);
            uiSourcesComboBox.Name = "uiSourcesComboBox";
            uiSourcesComboBox.Size = new Size(396, 23);
            uiSourcesComboBox.TabIndex = 4;
            // 
            // splitContainer1
            // 
            splitContainer1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            splitContainer1.Location = new Point(12, 12);
            splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            splitContainer1.Panel1.Controls.Add(panel1);
            splitContainer1.Panel1.Controls.Add(button1);
            splitContainer1.Panel1.Controls.Add(label2);
            splitContainer1.Panel1.Controls.Add(label1);
            splitContainer1.Panel1.Controls.Add(uiRelationToComboBox);
            splitContainer1.Panel1.Controls.Add(uiRelationFromComboBox);
            splitContainer1.Panel1.Controls.Add(uiMediaSourcePanel);
            splitContainer1.Panel1.Controls.Add(uiSourcesComboBox);
            splitContainer1.Panel1.Controls.Add(btnSync);
            splitContainer1.Panel1.Controls.Add(uiAddSourceButton);
            // 
            // splitContainer1.Panel2
            // 
            splitContainer1.Panel2.Controls.Add(mediaMatrixGridControl1);
            splitContainer1.Size = new Size(1133, 770);
            splitContainer1.SplitterDistance = 402;
            splitContainer1.TabIndex = 5;
            // 
            // panel1
            // 
            panel1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            panel1.AutoScroll = true;
            panel1.BackColor = SystemColors.ControlDark;
            panel1.Location = new Point(3, 600);
            panel1.Name = "panel1";
            panel1.Padding = new Padding(5);
            panel1.Size = new Size(396, 163);
            panel1.TabIndex = 1;
            // 
            // button1
            // 
            button1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            button1.Location = new Point(3, 562);
            button1.Name = "button1";
            button1.Size = new Size(396, 32);
            button1.TabIndex = 9;
            button1.Text = "add";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(3, 515);
            label2.Name = "label2";
            label2.Size = new Size(32, 15);
            label2.TabIndex = 8;
            label2.Text = "Куда";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(3, 471);
            label1.Name = "label1";
            label1.Size = new Size(45, 15);
            label1.TabIndex = 7;
            label1.Text = "Откуда";
            // 
            // uiRelationToComboBox
            // 
            uiRelationToComboBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            uiRelationToComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            uiRelationToComboBox.FormattingEnabled = true;
            uiRelationToComboBox.Location = new Point(3, 533);
            uiRelationToComboBox.Name = "uiRelationToComboBox";
            uiRelationToComboBox.Size = new Size(396, 23);
            uiRelationToComboBox.TabIndex = 6;
            // 
            // uiRelationFromComboBox
            // 
            uiRelationFromComboBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            uiRelationFromComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            uiRelationFromComboBox.FormattingEnabled = true;
            uiRelationFromComboBox.Location = new Point(3, 489);
            uiRelationFromComboBox.Name = "uiRelationFromComboBox";
            uiRelationFromComboBox.Size = new Size(396, 23);
            uiRelationFromComboBox.TabIndex = 5;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1157, 794);
            Controls.Add(splitContainer1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "MainForm";
            Text = "Медиа оркестратор";
            Load += MainForm_Load;
            splitContainer1.Panel1.ResumeLayout(false);
            splitContainer1.Panel1.PerformLayout();
            splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
            splitContainer1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel uiMediaSourcePanel;
        private Button btnSync;
        private MediaMatrixGridControl mediaMatrixGridControl1;
        private Button uiAddSourceButton;
        private ComboBox uiSourcesComboBox;
        private SplitContainer splitContainer1;
        private Panel panel1;
        private Button button1;
        private Label label2;
        private Label label1;
        private ComboBox uiRelationToComboBox;
        private ComboBox uiRelationFromComboBox;
    }
}

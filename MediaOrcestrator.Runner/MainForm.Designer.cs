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
            uMediaSourcePanel = new Panel();
            btnSync = new Button();
            mediaMatrixGridControl1 = new MediaMatrixGridControl();
            uiAddSourceButton = new Button();
            uiSourcesComboBox = new ComboBox();
            panel1 = new Panel();
            button1 = new Button();
            label2 = new Label();
            label1 = new Label();
            uiRelationToComboBox = new ComboBox();
            uiRelationFromComboBox = new ComboBox();
            tabControl1 = new TabControl();
            tabPage1 = new TabPage();
            tabPage2 = new TabPage();
            tabPage3 = new TabPage();
            tabPage4 = new TabPage();
            uiRelationViewModeCheckBox = new CheckBox();
            tabControl1.SuspendLayout();
            tabPage1.SuspendLayout();
            tabPage2.SuspendLayout();
            tabPage3.SuspendLayout();
            tabPage4.SuspendLayout();
            SuspendLayout();
            // 
            // uMediaSourcePanel
            // 
            uMediaSourcePanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            uMediaSourcePanel.AutoScroll = true;
            uMediaSourcePanel.BackColor = SystemColors.ControlDark;
            uMediaSourcePanel.Location = new Point(6, 35);
            uMediaSourcePanel.Name = "uMediaSourcePanel";
            uMediaSourcePanel.Padding = new Padding(5);
            uMediaSourcePanel.Size = new Size(1113, 663);
            uMediaSourcePanel.TabIndex = 0;
            uMediaSourcePanel.SizeChanged += uMediaSourcePanel_SizeChanged;
            // 
            // btnSync
            // 
            btnSync.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            btnSync.Location = new Point(281, 238);
            btnSync.Name = "btnSync";
            btnSync.Size = new Size(348, 32);
            btnSync.TabIndex = 1;
            btnSync.Text = "Синхронизировать";
            btnSync.UseVisualStyleBackColor = true;
            btnSync.Click += btnSync_Click;
            // 
            // mediaMatrixGridControl1
            // 
            mediaMatrixGridControl1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            mediaMatrixGridControl1.Location = new Point(3, 6);
            mediaMatrixGridControl1.Name = "mediaMatrixGridControl1";
            mediaMatrixGridControl1.Size = new Size(1116, 692);
            mediaMatrixGridControl1.TabIndex = 2;
            // 
            // uiAddSourceButton
            // 
            uiAddSourceButton.Location = new Point(360, 5);
            uiAddSourceButton.Name = "uiAddSourceButton";
            uiAddSourceButton.Size = new Size(348, 25);
            uiAddSourceButton.TabIndex = 3;
            uiAddSourceButton.Text = "Добавить источник";
            uiAddSourceButton.UseVisualStyleBackColor = true;
            uiAddSourceButton.Click += uiAddSourceButton_Click;
            // 
            // uiSourcesComboBox
            // 
            uiSourcesComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            uiSourcesComboBox.FormattingEnabled = true;
            uiSourcesComboBox.Location = new Point(6, 6);
            uiSourcesComboBox.Name = "uiSourcesComboBox";
            uiSourcesComboBox.Size = new Size(348, 23);
            uiSourcesComboBox.TabIndex = 4;
            // 
            // panel1
            // 
            panel1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            panel1.AutoScroll = true;
            panel1.BackColor = SystemColors.ControlDark;
            panel1.Location = new Point(3, 51);
            panel1.Name = "panel1";
            panel1.Padding = new Padding(5);
            panel1.Size = new Size(1119, 650);
            panel1.TabIndex = 1;
            // 
            // button1
            // 
            button1.Location = new Point(643, 21);
            button1.Name = "button1";
            button1.Size = new Size(314, 25);
            button1.TabIndex = 9;
            button1.Text = "add";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(323, 4);
            label2.Name = "label2";
            label2.Size = new Size(32, 15);
            label2.TabIndex = 8;
            label2.Text = "Куда";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(3, 4);
            label1.Name = "label1";
            label1.Size = new Size(45, 15);
            label1.TabIndex = 7;
            label1.Text = "Откуда";
            // 
            // uiRelationToComboBox
            // 
            uiRelationToComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            uiRelationToComboBox.FormattingEnabled = true;
            uiRelationToComboBox.Location = new Point(323, 22);
            uiRelationToComboBox.Name = "uiRelationToComboBox";
            uiRelationToComboBox.Size = new Size(314, 23);
            uiRelationToComboBox.TabIndex = 6;
            // 
            // uiRelationFromComboBox
            // 
            uiRelationFromComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            uiRelationFromComboBox.FormattingEnabled = true;
            uiRelationFromComboBox.Location = new Point(3, 22);
            uiRelationFromComboBox.Name = "uiRelationFromComboBox";
            uiRelationFromComboBox.Size = new Size(314, 23);
            uiRelationFromComboBox.TabIndex = 5;
            // 
            // tabControl1
            // 
            tabControl1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabControl1.Controls.Add(tabPage1);
            tabControl1.Controls.Add(tabPage2);
            tabControl1.Controls.Add(tabPage3);
            tabControl1.Controls.Add(tabPage4);
            tabControl1.Location = new Point(12, 12);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(1133, 770);
            tabControl1.TabIndex = 6;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(uiRelationViewModeCheckBox);
            tabPage1.Controls.Add(mediaMatrixGridControl1);
            tabPage1.Location = new Point(4, 24);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(1125, 742);
            tabPage1.TabIndex = 0;
            tabPage1.Text = "Фаилы";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // uiRelationViewModeCheckBox
            // 
            uiRelationViewModeCheckBox.AutoSize = true;
            uiRelationViewModeCheckBox.Location = new Point(6, 6);
            uiRelationViewModeCheckBox.Name = "uiRelationViewModeCheckBox";
            uiRelationViewModeCheckBox.Size = new Size(130, 19);
            uiRelationViewModeCheckBox.TabIndex = 3;
            uiRelationViewModeCheckBox.Text = "Режим по связям";
            uiRelationViewModeCheckBox.UseVisualStyleBackColor = true;
            uiRelationViewModeCheckBox.CheckedChanged += uiRelationViewModeCheckBox_CheckedChanged;
            // 
            // mediaMatrixGridControl1
            // 
            mediaMatrixGridControl1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            mediaMatrixGridControl1.Location = new Point(3, 31);
            mediaMatrixGridControl1.Name = "mediaMatrixGridControl1";
            mediaMatrixGridControl1.Size = new Size(1116, 667);
            mediaMatrixGridControl1.TabIndex = 2;
            // 
            // tabPage2
            // 
            tabPage2.Controls.Add(uiSourcesComboBox);
            tabPage2.Controls.Add(uiAddSourceButton);
            tabPage2.Controls.Add(uMediaSourcePanel);
            tabPage2.Location = new Point(4, 24);
            tabPage2.Name = "tabPage2";
            tabPage2.Padding = new Padding(3);
            tabPage2.Size = new Size(1125, 742);
            tabPage2.TabIndex = 1;
            tabPage2.Text = "Хранилища";
            tabPage2.UseVisualStyleBackColor = true;
            // 
            // tabPage3
            // 
            tabPage3.Controls.Add(panel1);
            tabPage3.Controls.Add(button1);
            tabPage3.Controls.Add(uiRelationFromComboBox);
            tabPage3.Controls.Add(label2);
            tabPage3.Controls.Add(uiRelationToComboBox);
            tabPage3.Controls.Add(label1);
            tabPage3.Location = new Point(4, 24);
            tabPage3.Name = "tabPage3";
            tabPage3.Size = new Size(1125, 742);
            tabPage3.TabIndex = 2;
            tabPage3.Text = "Связи";
            tabPage3.UseVisualStyleBackColor = true;
            // 
            // tabPage4
            // 
            tabPage4.Controls.Add(btnSync);
            tabPage4.Location = new Point(4, 24);
            tabPage4.Name = "tabPage4";
            tabPage4.Size = new Size(1125, 742);
            tabPage4.TabIndex = 3;
            tabPage4.Text = "Аудит";
            tabPage4.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1157, 794);
            Controls.Add(tabControl1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "MainForm";
            Text = "Медиа оркестратор";
            Load += MainForm_Load;
            tabControl1.ResumeLayout(false);
            tabPage1.ResumeLayout(false);
            tabPage2.ResumeLayout(false);
            tabPage3.ResumeLayout(false);
            tabPage3.PerformLayout();
            tabPage4.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Panel uMediaSourcePanel;
        private Button btnSync;
        private MediaMatrixGridControl mediaMatrixGridControl1;
        private Button uiAddSourceButton;
        private ComboBox uiSourcesComboBox;
        private Panel panel1;
        private Button button1;
        private Label label2;
        private Label label1;
        private ComboBox uiRelationToComboBox;
        private ComboBox uiRelationFromComboBox;
        private TabControl tabControl1;
        private TabPage tabPage1;
        private TabPage tabPage2;
        private TabPage tabPage3;
        private TabPage tabPage4;
        private CheckBox uiRelationViewModeCheckBox;
    }
}

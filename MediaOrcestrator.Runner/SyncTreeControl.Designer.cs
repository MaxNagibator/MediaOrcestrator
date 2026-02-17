namespace MediaOrcestrator.Runner
{
    partial class SyncTreeControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            uiTreeView = new TreeView();
            uiIconsImageList = new ImageList(components);
            uiExecuteButton = new Button();
            uiStopButton = new Button();
            uiTopPanel = new Panel();
            uiDeselectAllButton = new Button();
            uiSelectAllButton = new Button();
            uiStatusStrip = new StatusStrip();
            uiStatusLabel = new ToolStripStatusLabel();
            uiMainSplitContainer = new SplitContainer();
            uiLogRichTextBox = new RichTextBox();
            uiBottomPanel = new Panel();
            uiTopPanel.SuspendLayout();
            uiStatusStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)uiMainSplitContainer).BeginInit();
            uiMainSplitContainer.Panel1.SuspendLayout();
            uiMainSplitContainer.Panel2.SuspendLayout();
            uiMainSplitContainer.SuspendLayout();
            uiBottomPanel.SuspendLayout();
            SuspendLayout();
            // 
            // uiTreeView
            // 
            uiTreeView.CheckBoxes = true;
            uiTreeView.Dock = DockStyle.Fill;
            uiTreeView.ImageIndex = 0;
            uiTreeView.ImageList = uiIconsImageList;
            uiTreeView.Location = new Point(0, 0);
            uiTreeView.Name = "uiTreeView";
            uiTreeView.SelectedImageIndex = 0;
            uiTreeView.Size = new Size(796, 411);
            uiTreeView.TabIndex = 0;
            // 
            // uiIconsImageList
            // 
            uiIconsImageList.ColorDepth = ColorDepth.Depth32Bit;
            uiIconsImageList.ImageSize = new Size(16, 16);
            uiIconsImageList.TransparentColor = Color.Transparent;
            // 
            // uiExecuteButton
            // 
            uiExecuteButton.Dock = DockStyle.Left;
            uiExecuteButton.Location = new Point(0, 0);
            uiExecuteButton.Name = "uiExecuteButton";
            uiExecuteButton.Size = new Size(398, 40);
            uiExecuteButton.TabIndex = 1;
            uiExecuteButton.Text = "Выполнить выбранное";
            uiExecuteButton.UseVisualStyleBackColor = true;
            uiExecuteButton.Click += uiExecuteButton_Click;
            // 
            // uiStopButton
            // 
            uiStopButton.Dock = DockStyle.Fill;
            uiStopButton.Enabled = false;
            uiStopButton.Location = new Point(398, 0);
            uiStopButton.Name = "uiStopButton";
            uiStopButton.Size = new Size(398, 40);
            uiStopButton.TabIndex = 5;
            uiStopButton.Text = "Остановить";
            uiStopButton.UseVisualStyleBackColor = true;
            uiStopButton.Click += uiStopButton_Click;
            // 
            // uiTopPanel
            // 
            uiTopPanel.Controls.Add(uiDeselectAllButton);
            uiTopPanel.Controls.Add(uiSelectAllButton);
            uiTopPanel.Dock = DockStyle.Top;
            uiTopPanel.Location = new Point(0, 0);
            uiTopPanel.Name = "uiTopPanel";
            uiTopPanel.Size = new Size(796, 40);
            uiTopPanel.TabIndex = 2;
            // 
            // uiDeselectAllButton
            // 
            uiDeselectAllButton.Location = new Point(135, 6);
            uiDeselectAllButton.Name = "uiDeselectAllButton";
            uiDeselectAllButton.Size = new Size(120, 28);
            uiDeselectAllButton.TabIndex = 1;
            uiDeselectAllButton.Text = "Снять все";
            uiDeselectAllButton.UseVisualStyleBackColor = true;
            uiDeselectAllButton.Click += uiDeselectAllButton_Click;
            // 
            // uiSelectAllButton
            // 
            uiSelectAllButton.Location = new Point(9, 6);
            uiSelectAllButton.Name = "uiSelectAllButton";
            uiSelectAllButton.Size = new Size(120, 28);
            uiSelectAllButton.TabIndex = 0;
            uiSelectAllButton.Text = "Выбрать все";
            uiSelectAllButton.UseVisualStyleBackColor = true;
            uiSelectAllButton.Click += uiSelectAllButton_Click;
            // 
            // uiStatusStrip
            // 
            uiStatusStrip.Items.AddRange(new ToolStripItem[] { uiStatusLabel });
            uiStatusStrip.Location = new Point(0, 624);
            uiStatusStrip.Name = "uiStatusStrip";
            uiStatusStrip.Size = new Size(796, 22);
            uiStatusStrip.TabIndex = 3;
            uiStatusStrip.Text = "statusStrip1";
            // 
            // uiStatusLabel
            // 
            uiStatusLabel.Name = "uiStatusLabel";
            uiStatusLabel.Size = new Size(781, 17);
            uiStatusLabel.Spring = true;
            uiStatusLabel.Text = "Готов";
            uiStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // uiMainSplitContainer
            // 
            uiMainSplitContainer.Dock = DockStyle.Fill;
            uiMainSplitContainer.Location = new Point(0, 40);
            uiMainSplitContainer.Name = "uiMainSplitContainer";
            uiMainSplitContainer.Orientation = Orientation.Horizontal;
            // 
            // uiMainSplitContainer.Panel1
            // 
            uiMainSplitContainer.Panel1.Controls.Add(uiTreeView);
            // 
            // uiMainSplitContainer.Panel2
            // 
            uiMainSplitContainer.Panel2.Controls.Add(uiLogRichTextBox);
            uiMainSplitContainer.Size = new Size(796, 544);
            uiMainSplitContainer.SplitterDistance = 411;
            uiMainSplitContainer.TabIndex = 4;
            // 
            // uiLogRichTextBox
            // 
            uiLogRichTextBox.BackColor = Color.Black;
            uiLogRichTextBox.Dock = DockStyle.Fill;
            uiLogRichTextBox.Font = new Font("Consolas", 9F, FontStyle.Regular, GraphicsUnit.Point, 204);
            uiLogRichTextBox.ForeColor = Color.LightGray;
            uiLogRichTextBox.Location = new Point(0, 0);
            uiLogRichTextBox.Name = "uiLogRichTextBox";
            uiLogRichTextBox.ReadOnly = true;
            uiLogRichTextBox.Size = new Size(796, 129);
            uiLogRichTextBox.TabIndex = 0;
            uiLogRichTextBox.Text = "";
            // 
            // uiBottomPanel
            // 
            uiBottomPanel.Controls.Add(uiStopButton);
            uiBottomPanel.Controls.Add(uiExecuteButton);
            uiBottomPanel.Dock = DockStyle.Bottom;
            uiBottomPanel.Location = new Point(0, 584);
            uiBottomPanel.Name = "uiBottomPanel";
            uiBottomPanel.Size = new Size(796, 40);
            uiBottomPanel.TabIndex = 5;
            // 
            // SyncTreeControl
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(uiMainSplitContainer);
            Controls.Add(uiTopPanel);
            Controls.Add(uiBottomPanel);
            Controls.Add(uiStatusStrip);
            Name = "SyncTreeControl";
            Size = new Size(796, 646);
            uiTopPanel.ResumeLayout(false);
            uiStatusStrip.ResumeLayout(false);
            uiStatusStrip.PerformLayout();
            uiMainSplitContainer.Panel1.ResumeLayout(false);
            uiMainSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)uiMainSplitContainer).EndInit();
            uiMainSplitContainer.ResumeLayout(false);
            uiBottomPanel.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private TreeView uiTreeView;
        private Button uiExecuteButton;
        private Panel uiTopPanel;
        private Button uiDeselectAllButton;
        private Button uiSelectAllButton;
        private StatusStrip uiStatusStrip;
        private ToolStripStatusLabel uiStatusLabel;
        private SplitContainer uiMainSplitContainer;
        private RichTextBox uiLogRichTextBox;
        private ImageList uiIconsImageList;
        private Button uiStopButton;
        private Panel uiBottomPanel;
    }
}

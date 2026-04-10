namespace MediaOrcestrator.Runner
{
    partial class AuditSourceRow
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            uiMainLayout = new TableLayoutPanel();
            uiTitleContainer = new Panel();
            uiTitleLabel = new Label();
            uiTypeLabel = new Label();
            uiProgressLabel = new Label();
            uiLastSyncLabel = new Label();
            uiFullSyncButton = new Button();
            uiQuickSyncButton = new Button();
            uiNewSyncButton = new Button();
            uiRowToolTip = new ToolTip(components);
            uiMainLayout.SuspendLayout();
            uiTitleContainer.SuspendLayout();
            SuspendLayout();
            //
            // uiMainLayout
            //
            uiMainLayout.ColumnCount = 6;
            uiMainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            uiMainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            uiMainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150F));
            uiMainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
            uiMainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
            uiMainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110F));
            uiMainLayout.Controls.Add(uiTitleContainer, 0, 0);
            uiMainLayout.Controls.Add(uiProgressLabel, 1, 0);
            uiMainLayout.Controls.Add(uiLastSyncLabel, 2, 0);
            uiMainLayout.Controls.Add(uiFullSyncButton, 3, 0);
            uiMainLayout.Controls.Add(uiQuickSyncButton, 4, 0);
            uiMainLayout.Controls.Add(uiNewSyncButton, 5, 0);
            uiMainLayout.Dock = DockStyle.Fill;
            uiMainLayout.Location = new Point(0, 0);
            uiMainLayout.Name = "uiMainLayout";
            uiMainLayout.RowCount = 1;
            uiMainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            uiMainLayout.Size = new Size(900, 52);
            uiMainLayout.TabIndex = 0;
            //
            // uiTitleContainer
            //
            uiTitleContainer.Controls.Add(uiTitleLabel);
            uiTitleContainer.Controls.Add(uiTypeLabel);
            uiTitleContainer.Dock = DockStyle.Fill;
            uiTitleContainer.Location = new Point(3, 3);
            uiTitleContainer.Name = "uiTitleContainer";
            uiTitleContainer.Size = new Size(399, 46);
            uiTitleContainer.TabIndex = 0;
            //
            // uiTitleLabel
            //
            uiTitleLabel.AutoEllipsis = true;
            uiTitleLabel.Dock = DockStyle.Top;
            uiTitleLabel.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold);
            uiTitleLabel.Location = new Point(0, 0);
            uiTitleLabel.Name = "uiTitleLabel";
            uiTitleLabel.Size = new Size(399, 22);
            uiTitleLabel.TabIndex = 0;
            uiTitleLabel.Text = "Источник";
            //
            // uiTypeLabel
            //
            uiTypeLabel.AutoEllipsis = true;
            uiTypeLabel.Dock = DockStyle.Bottom;
            uiTypeLabel.ForeColor = Color.Gray;
            uiTypeLabel.Location = new Point(0, 31);
            uiTypeLabel.Name = "uiTypeLabel";
            uiTypeLabel.Size = new Size(399, 15);
            uiTypeLabel.TabIndex = 1;
            uiTypeLabel.Text = "тип";
            //
            // uiProgressLabel
            //
            uiProgressLabel.AutoEllipsis = true;
            uiProgressLabel.Dock = DockStyle.Fill;
            uiProgressLabel.ForeColor = Color.DimGray;
            uiProgressLabel.Location = new Point(408, 0);
            uiProgressLabel.Name = "uiProgressLabel";
            uiProgressLabel.Size = new Size(264, 52);
            uiProgressLabel.TabIndex = 1;
            uiProgressLabel.Text = "—";
            uiProgressLabel.TextAlign = ContentAlignment.MiddleLeft;
            //
            // uiLastSyncLabel
            //
            uiLastSyncLabel.AutoEllipsis = true;
            uiLastSyncLabel.Dock = DockStyle.Fill;
            uiLastSyncLabel.ForeColor = Color.DimGray;
            uiLastSyncLabel.Name = "uiLastSyncLabel";
            uiLastSyncLabel.TabIndex = 5;
            uiLastSyncLabel.Text = "не синхронизировано";
            uiLastSyncLabel.TextAlign = ContentAlignment.MiddleLeft;
            //
            // uiFullSyncButton
            //
            uiFullSyncButton.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            uiFullSyncButton.Location = new Point(678, 14);
            uiFullSyncButton.Name = "uiFullSyncButton";
            uiFullSyncButton.Size = new Size(104, 23);
            uiFullSyncButton.TabIndex = 2;
            uiFullSyncButton.Text = "Полная";
            uiFullSyncButton.UseVisualStyleBackColor = true;
            uiFullSyncButton.Click += uiFullSyncButton_Click;
            //
            // uiQuickSyncButton
            //
            uiQuickSyncButton.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            uiQuickSyncButton.Location = new Point(788, 14);
            uiQuickSyncButton.Name = "uiQuickSyncButton";
            uiQuickSyncButton.Size = new Size(104, 23);
            uiQuickSyncButton.TabIndex = 3;
            uiQuickSyncButton.Text = "Быстрая";
            uiQuickSyncButton.UseVisualStyleBackColor = true;
            uiQuickSyncButton.Click += uiQuickSyncButton_Click;
            //
            // uiNewSyncButton
            //
            uiNewSyncButton.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            uiNewSyncButton.Location = new Point(898, 14);
            uiNewSyncButton.Name = "uiNewSyncButton";
            uiNewSyncButton.Size = new Size(104, 23);
            uiNewSyncButton.TabIndex = 4;
            uiNewSyncButton.Text = "Новые";
            uiNewSyncButton.UseVisualStyleBackColor = true;
            uiNewSyncButton.Click += uiNewSyncButton_Click;
            //
            // AuditSourceRow
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.WhiteSmoke;
            BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(uiMainLayout);
            Margin = new Padding(3);
            Name = "AuditSourceRow";
            Size = new Size(900, 52);
            uiMainLayout.ResumeLayout(false);
            uiTitleContainer.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel uiMainLayout;
        private Panel uiTitleContainer;
        private Label uiTitleLabel;
        private Label uiTypeLabel;
        private Label uiProgressLabel;
        private Label uiLastSyncLabel;
        private Button uiFullSyncButton;
        private Button uiQuickSyncButton;
        private Button uiNewSyncButton;
        private ToolTip uiRowToolTip;
    }
}

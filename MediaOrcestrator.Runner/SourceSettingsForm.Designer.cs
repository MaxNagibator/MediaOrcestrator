namespace MediaOrcestrator.Runner;

partial class SourceSettingsForm
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
        uiMainLayout = new TableLayoutPanel();
        uiNameLabel = new Label();
        uiNameTextBox = new TextBox();
        uiSettingsPanel = new Panel();
        uiCreateButton = new Button();
        uiMainLayout.SuspendLayout();
        SuspendLayout();
        // 
        // uiMainLayout
        // 
        uiMainLayout.ColumnCount = 1;
        uiMainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        uiMainLayout.Controls.Add(uiNameLabel, 0, 0);
        uiMainLayout.Controls.Add(uiNameTextBox, 0, 1);
        uiMainLayout.Controls.Add(uiSettingsPanel, 0, 2);
        uiMainLayout.Controls.Add(uiCreateButton, 0, 3);
        uiMainLayout.Dock = DockStyle.Fill;
        uiMainLayout.Location = new Point(10, 10);
        uiMainLayout.Name = "uiMainLayout";
        uiMainLayout.Padding = new Padding(10);
        uiMainLayout.RowCount = 4;
        uiMainLayout.RowStyles.Add(new RowStyle());
        uiMainLayout.RowStyles.Add(new RowStyle());
        uiMainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        uiMainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
        uiMainLayout.Size = new Size(602, 518);
        uiMainLayout.TabIndex = 0;
        // 
        // uiNameLabel
        // 
        uiNameLabel.AutoSize = true;
        uiNameLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        uiNameLabel.Location = new Point(13, 10);
        uiNameLabel.Name = "uiNameLabel";
        uiNameLabel.Size = new Size(127, 15);
        uiNameLabel.TabIndex = 2;
        uiNameLabel.Text = "Название источника";
        // 
        // uiNameTextBox
        // 
        uiNameTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        uiNameTextBox.Location = new Point(13, 28);
        uiNameTextBox.Margin = new Padding(3, 3, 3, 15);
        uiNameTextBox.Name = "uiNameTextBox";
        uiNameTextBox.Size = new Size(576, 23);
        uiNameTextBox.TabIndex = 1;
        // 
        // uiSettingsPanel
        // 
        uiSettingsPanel.AutoScroll = true;
        uiSettingsPanel.BackColor = Color.WhiteSmoke;
        uiSettingsPanel.Dock = DockStyle.Fill;
        uiSettingsPanel.Location = new Point(13, 69);
        uiSettingsPanel.Name = "uiSettingsPanel";
        uiSettingsPanel.Size = new Size(576, 391);
        uiSettingsPanel.TabIndex = 0;
        // 
        // uiCreateButton
        // 
        uiCreateButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        uiCreateButton.FlatStyle = FlatStyle.System;
        uiCreateButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        uiCreateButton.Location = new Point(416, 470);
        uiCreateButton.Name = "uiCreateButton";
        uiCreateButton.Size = new Size(173, 35);
        uiCreateButton.TabIndex = 3;
        uiCreateButton.Text = "Создать источник";
        uiCreateButton.UseVisualStyleBackColor = true;
        uiCreateButton.Click += uiCreateButton_Click;
        // 
        // SourceSettingsForm
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        BackColor = Color.White;
        ClientSize = new Size(622, 538);
        Controls.Add(uiMainLayout);
        Name = "SourceSettingsForm";
        Padding = new Padding(10);
        StartPosition = FormStartPosition.CenterParent;
        Text = "Настройки источника";
        Load += SourceSettingsForm_Load;
        uiMainLayout.ResumeLayout(false);
        uiMainLayout.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel uiMainLayout;
    private Panel uiSettingsPanel;
    private TextBox uiNameTextBox;
    private Label uiNameLabel;
    private Button uiCreateButton;
}

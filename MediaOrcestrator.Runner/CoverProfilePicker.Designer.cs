namespace MediaOrcestrator.Runner;

partial class CoverProfilePicker
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null)
        {
            components.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        uiLayout = new TableLayoutPanel();
        uiCombo = new ComboBox();
        uiSetupButton = new Button();
        uiLayout.SuspendLayout();
        SuspendLayout();
        //
        // uiLayout
        //
        uiLayout.ColumnCount = 2;
        uiLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        uiLayout.ColumnStyles.Add(new ColumnStyle());
        uiLayout.Controls.Add(uiCombo, 0, 0);
        uiLayout.Controls.Add(uiSetupButton, 1, 0);
        uiLayout.Dock = DockStyle.Fill;
        uiLayout.Margin = new Padding(0);
        uiLayout.Name = "uiLayout";
        uiLayout.RowCount = 1;
        uiLayout.RowStyles.Add(new RowStyle());
        uiLayout.TabIndex = 0;
        //
        // uiCombo
        //
        uiCombo.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        uiCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        uiCombo.FormattingEnabled = true;
        uiCombo.Margin = new Padding(0, 0, 6, 0);
        uiCombo.Name = "uiCombo";
        uiCombo.TabIndex = 0;
        uiCombo.SelectedIndexChanged += uiCombo_SelectedIndexChanged;
        //
        // uiSetupButton
        //
        uiSetupButton.AutoSize = true;
        uiSetupButton.Margin = new Padding(0);
        uiSetupButton.Name = "uiSetupButton";
        uiSetupButton.TabIndex = 1;
        uiSetupButton.Text = "Настроить…";
        uiSetupButton.UseVisualStyleBackColor = true;
        uiSetupButton.Click += uiSetupButton_Click;
        //
        // CoverProfilePicker
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        Controls.Add(uiLayout);
        Margin = new Padding(0);
        Name = "CoverProfilePicker";
        Size = new Size(320, 25);
        uiLayout.ResumeLayout(false);
        uiLayout.PerformLayout();
        ResumeLayout(false);
    }

    private TableLayoutPanel uiLayout;
    private ComboBox uiCombo;
    private Button uiSetupButton;
}

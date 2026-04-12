namespace MediaOrcestrator.Runner;

partial class UpdateForm
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
        uiVersionLabel = new Label();
        uiSizeLabel = new Label();
        uiReleaseNotesLabel = new Label();
        uiReleaseNotesBrowser = new WebBrowser();
        uiStatusLabel = new Label();
        uiProgressBar = new ProgressBar();
        uiButtonsPanel = new FlowLayoutPanel();
        uiLaterButton = new Button();
        uiUpdateButton = new Button();
        uiCancelButton = new Button();
        uiMainLayout.SuspendLayout();
        uiButtonsPanel.SuspendLayout();
        SuspendLayout();
        // 
        // uiMainLayout
        // 
        uiMainLayout.ColumnCount = 1;
        uiMainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        uiMainLayout.Controls.Add(uiVersionLabel, 0, 0);
        uiMainLayout.Controls.Add(uiSizeLabel, 0, 1);
        uiMainLayout.Controls.Add(uiReleaseNotesLabel, 0, 2);
        uiMainLayout.Controls.Add(uiReleaseNotesBrowser, 0, 3);
        uiMainLayout.Controls.Add(uiStatusLabel, 0, 4);
        uiMainLayout.Controls.Add(uiProgressBar, 0, 5);
        uiMainLayout.Controls.Add(uiButtonsPanel, 0, 6);
        uiMainLayout.Dock = DockStyle.Fill;
        uiMainLayout.Location = new Point(0, 0);
        uiMainLayout.Name = "uiMainLayout";
        uiMainLayout.Padding = new Padding(12);
        uiMainLayout.RowCount = 7;
        uiMainLayout.RowStyles.Add(new RowStyle());
        uiMainLayout.RowStyles.Add(new RowStyle());
        uiMainLayout.RowStyles.Add(new RowStyle());
        uiMainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        uiMainLayout.RowStyles.Add(new RowStyle());
        uiMainLayout.RowStyles.Add(new RowStyle());
        uiMainLayout.RowStyles.Add(new RowStyle());
        uiMainLayout.Size = new Size(560, 460);
        uiMainLayout.TabIndex = 0;
        // 
        // uiVersionLabel
        // 
        uiVersionLabel.AutoSize = true;
        uiVersionLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
        uiVersionLabel.Location = new Point(12, 12);
        uiVersionLabel.Margin = new Padding(0, 0, 0, 4);
        uiVersionLabel.Name = "uiVersionLabel";
        uiVersionLabel.Size = new Size(195, 21);
        uiVersionLabel.TabIndex = 0;
        uiVersionLabel.Text = "Доступна новая версия";
        // 
        // uiSizeLabel
        // 
        uiSizeLabel.AutoSize = true;
        uiSizeLabel.Location = new Point(12, 37);
        uiSizeLabel.Margin = new Padding(0, 0, 0, 8);
        uiSizeLabel.Name = "uiSizeLabel";
        uiSizeLabel.Size = new Size(50, 15);
        uiSizeLabel.TabIndex = 1;
        uiSizeLabel.Text = "Размер:";
        // 
        // uiReleaseNotesLabel
        // 
        uiReleaseNotesLabel.AutoSize = true;
        uiReleaseNotesLabel.Location = new Point(12, 60);
        uiReleaseNotesLabel.Margin = new Padding(0, 0, 0, 2);
        uiReleaseNotesLabel.Name = "uiReleaseNotesLabel";
        uiReleaseNotesLabel.Size = new Size(72, 15);
        uiReleaseNotesLabel.TabIndex = 2;
        uiReleaseNotesLabel.Text = "Что нового:";
        // 
        // uiReleaseNotesBrowser
        // 
        uiReleaseNotesBrowser.Dock = DockStyle.Fill;
        uiReleaseNotesBrowser.IsWebBrowserContextMenuEnabled = false;
        uiReleaseNotesBrowser.Location = new Point(15, 80);
        uiReleaseNotesBrowser.MinimumSize = new Size(20, 20);
        uiReleaseNotesBrowser.Name = "uiReleaseNotesBrowser";
        uiReleaseNotesBrowser.ScriptErrorsSuppressed = true;
        uiReleaseNotesBrowser.Size = new Size(530, 271);
        uiReleaseNotesBrowser.TabIndex = 3;
        // 
        // uiStatusLabel
        // 
        uiStatusLabel.AutoSize = true;
        uiStatusLabel.Location = new Point(12, 362);
        uiStatusLabel.Margin = new Padding(0, 8, 0, 4);
        uiStatusLabel.Name = "uiStatusLabel";
        uiStatusLabel.Size = new Size(152, 15);
        uiStatusLabel.TabIndex = 4;
        uiStatusLabel.Text = "Скачивание обновления...";
        uiStatusLabel.Visible = false;
        // 
        // uiProgressBar
        // 
        uiProgressBar.Dock = DockStyle.Fill;
        uiProgressBar.Location = new Point(12, 381);
        uiProgressBar.Margin = new Padding(0, 0, 0, 4);
        uiProgressBar.Name = "uiProgressBar";
        uiProgressBar.Size = new Size(536, 22);
        uiProgressBar.TabIndex = 5;
        uiProgressBar.Visible = false;
        // 
        // uiButtonsPanel
        // 
        uiButtonsPanel.AutoSize = true;
        uiButtonsPanel.Controls.Add(uiLaterButton);
        uiButtonsPanel.Controls.Add(uiUpdateButton);
        uiButtonsPanel.Controls.Add(uiCancelButton);
        uiButtonsPanel.Dock = DockStyle.Fill;
        uiButtonsPanel.FlowDirection = FlowDirection.RightToLeft;
        uiButtonsPanel.Location = new Point(15, 410);
        uiButtonsPanel.Name = "uiButtonsPanel";
        uiButtonsPanel.Padding = new Padding(0, 4, 0, 0);
        uiButtonsPanel.Size = new Size(530, 35);
        uiButtonsPanel.TabIndex = 6;
        // 
        // uiLaterButton
        // 
        uiLaterButton.AutoSize = true;
        uiLaterButton.DialogResult = DialogResult.No;
        uiLaterButton.Location = new Point(452, 7);
        uiLaterButton.Name = "uiLaterButton";
        uiLaterButton.Size = new Size(75, 25);
        uiLaterButton.TabIndex = 0;
        uiLaterButton.Text = "Позже";
        uiLaterButton.UseVisualStyleBackColor = true;
        // 
        // uiUpdateButton
        // 
        uiUpdateButton.AutoSize = true;
        uiUpdateButton.Location = new Point(371, 7);
        uiUpdateButton.Name = "uiUpdateButton";
        uiUpdateButton.Size = new Size(75, 25);
        uiUpdateButton.TabIndex = 1;
        uiUpdateButton.Text = "Обновить";
        uiUpdateButton.UseVisualStyleBackColor = true;
        uiUpdateButton.Click += uiUpdateButton_Click;
        // 
        // uiCancelButton
        // 
        uiCancelButton.AutoSize = true;
        uiCancelButton.Location = new Point(290, 7);
        uiCancelButton.Name = "uiCancelButton";
        uiCancelButton.Size = new Size(75, 25);
        uiCancelButton.TabIndex = 2;
        uiCancelButton.Text = "Отмена";
        uiCancelButton.UseVisualStyleBackColor = true;
        uiCancelButton.Visible = false;
        uiCancelButton.Click += uiCancelButton_Click;
        // 
        // UpdateForm
        // 
        AcceptButton = uiUpdateButton;
        CancelButton = uiLaterButton;
        ClientSize = new Size(560, 460);
        Controls.Add(uiMainLayout);
        MaximizeBox = false;
        MinimizeBox = false;
        MinimumSize = new Size(420, 340);
        Name = "UpdateForm";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Обновление приложения";
        Load += UpdateForm_Load;
        uiMainLayout.ResumeLayout(false);
        uiMainLayout.PerformLayout();
        uiButtonsPanel.ResumeLayout(false);
        uiButtonsPanel.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel uiMainLayout;
    private Label uiVersionLabel;
    private Label uiSizeLabel;
    private Label uiReleaseNotesLabel;
    private WebBrowser uiReleaseNotesBrowser;
    private Label uiStatusLabel;
    private ProgressBar uiProgressBar;
    private FlowLayoutPanel uiButtonsPanel;
    private Button uiLaterButton;
    private Button uiUpdateButton;
    private Button uiCancelButton;
}

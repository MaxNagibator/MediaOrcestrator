namespace MediaOrcestrator.Runner;

public sealed partial class CommentsAppearanceDialog : Form
{
    private const string DefaultReplyPrefix = "{name}, ";

    private static readonly ReplyPrefixOption CustomReplyOption = new("Свой вариант", null);

    private static readonly ReplyPrefixOption[] ReplyPrefixOptions =
    [
        new("Имя, ", "{name}, "),
        new("@Имя ", "@{name} "),
        new("Имя ", "{name} "),
        new("Без префикса", ""),
        CustomReplyOption,
    ];

    public CommentsAppearanceDialog()
    {
        InitializeComponent();
        uiReplyPresetCombo.Items.AddRange(ReplyPrefixOptions);
    }

    public CommentsAppearanceDialog(CommentsAppearance appearance, string replyPrefixTemplate) : this()
    {
        LoadValues(appearance);
        LoadReplyPrefix(replyPrefixTemplate);
    }

    public CommentsAppearance Appearance
    {
        get
        {
            var result = new CommentsAppearance
            {
                BaseFontSize = (int)uiBaseNumeric.Value,
                CommentFontSize = (int)uiCommentNumeric.Value,
                AuthorFontSize = (int)uiAuthorNumeric.Value,
                MetaFontSize = (int)uiMetaNumeric.Value,
                MediaTitleFontSize = (int)uiMediaTitleNumeric.Value,
                BadgeFontSize = (int)uiBadgeNumeric.Value,
                LineHeight = (double)uiLineHeightNumeric.Value,
            };

            result.Normalize();
            return result;
        }
    }

    public string ReplyPrefixTemplate =>
        uiReplyPresetCombo.SelectedItem is ReplyPrefixOption { Template: { } template }
            ? template
            : uiReplyCustomTextBox.Text;

    private void uiResetButton_Click(object? sender, EventArgs e)
    {
        LoadValues(new());
        LoadReplyPrefix(DefaultReplyPrefix);
    }

    private void uiReplyPresetCombo_SelectedIndexChanged(object? sender, EventArgs e)
    {
        var custom = uiReplyPresetCombo.SelectedItem is not ReplyPrefixOption { Template: not null };
        uiReplyCustomTextBox.Enabled = custom;

        if (!custom && uiReplyPresetCombo.SelectedItem is ReplyPrefixOption { Template: { } template })
        {
            uiReplyCustomTextBox.Text = template;
        }
    }

    private void LoadValues(CommentsAppearance appearance)
    {
        uiBaseNumeric.Value = ClampToFont(appearance.BaseFontSize);
        uiCommentNumeric.Value = ClampToFont(appearance.CommentFontSize);
        uiAuthorNumeric.Value = ClampToFont(appearance.AuthorFontSize);
        uiMetaNumeric.Value = ClampToFont(appearance.MetaFontSize);
        uiMediaTitleNumeric.Value = ClampToFont(appearance.MediaTitleFontSize);
        uiBadgeNumeric.Value = ClampToFont(appearance.BadgeFontSize);
        uiLineHeightNumeric.Value = Math.Clamp((decimal)appearance.LineHeight,
            uiLineHeightNumeric.Minimum,
            uiLineHeightNumeric.Maximum);
    }

    private void LoadReplyPrefix(string template)
    {
        template ??= DefaultReplyPrefix;

        var preset = Array.Find(ReplyPrefixOptions,
            o => o.Template != null && string.Equals(o.Template, template, StringComparison.Ordinal));

        if (preset != null)
        {
            uiReplyPresetCombo.SelectedItem = preset;
        }
        else
        {
            uiReplyPresetCombo.SelectedItem = CustomReplyOption;
            uiReplyCustomTextBox.Text = template;
        }
    }

    private decimal ClampToFont(int value)
    {
        return Math.Clamp(value, (int)uiBaseNumeric.Minimum, (int)uiBaseNumeric.Maximum);
    }

    private sealed record ReplyPrefixOption(string Label, string? Template)
    {
        public override string ToString()
        {
            return Label;
        }
    }
}

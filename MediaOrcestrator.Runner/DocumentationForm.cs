using Markdig;
using System.Reflection;

namespace MediaOrcestrator.Runner;

public partial class DocumentationForm : Form
{
    private const string TemplateResourceName = "MediaOrcestrator.Runner.Resources.docs.template.html";

    private static readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .Build();

    private static readonly string _htmlTemplate = LoadTemplate();

    private readonly string _markdownContent = "";
    private readonly string _basePath = "";

    public DocumentationForm()
    {
        InitializeComponent();
    }

    public DocumentationForm(string title, string markdownContent, string basePath) : this()
    {
        Text = title;
        _markdownContent = markdownContent;
        _basePath = basePath;
    }

    public static void ShowAppDoc(IWin32Window? owner, string title, string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "docs", fileName);
        ShowForFile(owner, title, path);
    }

    public static void ShowForFile(IWin32Window? owner, string title, string markdownPath)
    {
        if (!File.Exists(markdownPath))
        {
            MessageBox.Show($"Файл справки не найден: {markdownPath}",
                "Справка",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }

        string markdown;

        try
        {
            markdown = File.ReadAllText(markdownPath);
        }
        catch (IOException ex)
        {
            MessageBox.Show($"Не удалось прочитать файл справки: {ex.Message}",
                "Справка",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);

            return;
        }

        var basePath = Path.GetDirectoryName(Path.GetFullPath(markdownPath)) ?? AppContext.BaseDirectory;
        var form = new DocumentationForm(title, markdown, basePath);
        form.FormClosed += (_, _) => form.Dispose();
        form.Show(owner);
    }

    internal static string RenderMarkdown(string markdown, string basePath)
    {
        var htmlBody = Markdown.ToHtml(markdown, _pipeline);
        var baseUri = string.IsNullOrEmpty(basePath)
            ? new Uri(AppContext.BaseDirectory).AbsoluteUri
            : new Uri(Path.GetFullPath(basePath) + Path.DirectorySeparatorChar).AbsoluteUri;

        return _htmlTemplate
            .Replace("{{baseUri}}", baseUri)
            .Replace("{{body}}", htmlBody);
    }

    private void DocumentationForm_Load(object? sender, EventArgs e)
    {
        uiWebBrowser.DocumentText = RenderMarkdown(_markdownContent, _basePath);
    }

    private static string LoadTemplate()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(TemplateResourceName)
                           ?? throw new InvalidOperationException($"Не найден embedded ресурс {TemplateResourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}

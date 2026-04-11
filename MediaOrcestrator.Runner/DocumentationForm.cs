using Markdig;

namespace MediaOrcestrator.Runner;

public partial class DocumentationForm : Form
{
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

    private void DocumentationForm_Load(object? sender, EventArgs e)
    {
        uiWebBrowser.DocumentText = RenderMarkdown(_markdownContent, _basePath);
    }

    private static string RenderMarkdown(string markdown, string basePath)
    {
        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

        var htmlBody = Markdown.ToHtml(markdown, pipeline);
        var baseUri = new Uri(basePath + Path.DirectorySeparatorChar).AbsoluteUri;

        return $$"""
                 <!DOCTYPE html>
                 <html>
                 <head>
                     <meta charset="utf-8" />
                     <base href="{{baseUri}}" />
                     <style>
                         body {
                             font-family: 'Segoe UI', sans-serif;
                             font-size: 14px;
                             line-height: 1.6;
                             color: #333;
                             max-width: 780px;
                             margin: 0 auto;
                             padding: 20px;
                         }
                         h1 { font-size: 22px; border-bottom: 1px solid #ddd; padding-bottom: 8px; }
                         h2 { font-size: 18px; margin-top: 24px; }
                         h3 { font-size: 15px; }
                         code { background: #f4f4f4; padding: 2px 5px; border-radius: 3px; font-size: 13px; }
                         pre { background: #f4f4f4; padding: 12px; border-radius: 5px; overflow-x: auto; }
                         pre code { padding: 0; background: none; }
                         table { border-collapse: collapse; width: 100%; margin: 12px 0; }
                         th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }
                         th { background: #f8f8f8; font-weight: 600; }
                         img { max-width: 100%; height: auto; border: 1px solid #ddd; border-radius: 4px; margin: 8px 0; }
                         a { color: #0066cc; }
                     </style>
                 </head>
                 <body>
                 {{htmlBody}}
                 </body>
                 </html>
                 """;
    }
}

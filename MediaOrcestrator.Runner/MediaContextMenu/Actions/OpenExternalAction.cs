using MediaOrcestrator.Modules;
using System.Diagnostics;

namespace MediaOrcestrator.Runner.MediaContextMenu.Actions;

internal sealed class OpenExternalAction : IMediaMenuAction
{
    private const int ConfirmThreshold = 5;

    public int Order => 650;

    public IEnumerable<MenuItemSpec> Build(MediaSelection selection, MediaActionContext ctx)
    {
        var sources = selection.SpecificSource != null
            ? [selection.SpecificSource]
            : ctx.Orcestrator.GetSources();

        foreach (var source in sources)
        {
            if (source.Type == null)
            {
                continue;
            }

            var uris = new List<Uri>();

            foreach (var media in selection.Items)
            {
                var link = media.Sources.FirstOrDefault(s => s.SourceId == source.Id
                                                             && s.Status != MediaStatus.Skipped
                                                             && !string.IsNullOrEmpty(s.ExternalId));

                if (link == null)
                {
                    continue;
                }

                var metadata = media.Metadata.ForSource(source.Id);
                var uri = source.Type.GetExternalUri(link.ExternalId, source.Settings, metadata);

                if (uri != null)
                {
                    uris.Add(uri);
                }
            }

            if (uris.Count == 0)
            {
                continue;
            }

            var text = selection.IsBatch
                ? $"Открыть в {source.TitleFull} ({uris.Count})"
                : $"Открыть: {source.TitleFull}";

            yield return new(text, MenuIcons.Open)
            {
                Execute = () => OpenAll(uris, ctx),
            };
        }
    }

    private static Task OpenAll(IReadOnlyList<Uri> uris, MediaActionContext ctx)
    {
        if (uris.Count == 0)
        {
            return Task.CompletedTask;
        }

        if (uris.Count > ConfirmThreshold)
        {
            var confirm = MessageBox.Show(ctx.Ui.Owner,
                $"Открыть {uris.Count} внешних ссылок в браузере?",
                "Подтверждение",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                return Task.CompletedTask;
            }
        }

        foreach (var uri in uris)
        {
            Process.Start(new ProcessStartInfo(uri.ToString()) { UseShellExecute = true });
        }

        return Task.CompletedTask;
    }
}

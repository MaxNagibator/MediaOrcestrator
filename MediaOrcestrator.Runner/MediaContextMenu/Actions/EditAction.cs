namespace MediaOrcestrator.Runner.MediaContextMenu.Actions;

internal sealed class EditAction : IMediaMenuAction
{
    public int Order => 500;

    public IEnumerable<MenuItemSpec> Build(MediaSelection selection, MediaActionContext ctx)
    {
        var renameText = selection.IsBatch
            ? $"Пакетное переименование ({selection.Count})..."
            : "Переименовать...";

        yield return new(renameText, MenuIcons.Rename)
        {
            Execute = () => RunRename(selection, ctx),
        };

        var previewText = selection.IsBatch
            ? $"Обновить превью ({selection.Count})..."
            : "Обновить превью...";

        yield return new(previewText, MenuIcons.Preview)
        {
            Execute = () => RunPreview(selection, ctx),
        };

        if (selection.Count >= 2)
        {
            yield return new($"Объединить ({selection.Count})", MenuIcons.Merge)
            {
                Execute = () =>
                {
                    MergeRunner.Run(selection.InSelectionOrder, ctx);
                    return Task.CompletedTask;
                },
            };
        }
    }

    private static Task RunRename(MediaSelection selection, MediaActionContext ctx)
    {
        var medias = selection.Items.ToList();

        var gridSourceIds = selection.GridSources?.Select(s => s.Id).ToHashSet(StringComparer.Ordinal);
        var form = new BatchRenameForm(medias, ctx.BatchRenameService, ctx.Logger, ctx.ActionHolder, selection.SpecificSource, gridSourceIds);
        form.DataChanged += (_, _) => ctx.Ui.NotifyDataChanged();
        form.Show(ctx.Ui.Owner);

        return Task.CompletedTask;
    }

    private static Task RunPreview(MediaSelection selection, MediaActionContext ctx)
    {
        var medias = selection.Items.ToList();
        var form = new BatchPreviewForm(medias,
            ctx.BatchPreviewService,
            ctx.CoverGenerator,
            ctx.CoverTemplateStore,
            ctx.Logger,
            ctx.ActionHolder);

        form.DataChanged += (_, _) => ctx.Ui.NotifyDataChanged();
        form.Show(ctx.Ui.Owner);

        return Task.CompletedTask;
    }
}

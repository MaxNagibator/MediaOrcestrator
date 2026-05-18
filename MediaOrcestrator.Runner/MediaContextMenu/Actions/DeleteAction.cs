using MediaOrcestrator.Domain;
using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.Runner.MediaContextMenu.Actions;

internal sealed class DeleteAction : IMediaMenuAction
{
    public int Order => 700;

    public IEnumerable<MenuItemSpec> Build(MediaSelection selection, MediaActionContext ctx)
    {
        var sources = selection.SpecificSource != null
            ? [selection.SpecificSource]
            : ctx.Orcestrator.GetSources().Where(s => !s.IsDisable).ToList();

        foreach (var source in sources)
        {
            if (source.IsDisable)
            {
                continue;
            }

            var eligible = selection.Items
                .Where(m => m.Sources.Any(s => s.SourceId == source.Id && s.Status != MediaStatus.Skipped))
                .ToList();

            if (eligible.Count == 0)
            {
                continue;
            }

            var text = selection.IsBatch
                ? $"Удалить из {source.TitleFull} ({eligible.Count})"
                : $"Удалить из {source.TitleFull}";

            yield return new(text, MenuIcons.Delete)
            {
                Execute = () => ExecuteAsync(eligible, source, ctx),
            };
        }
    }

    private static Task ExecuteAsync(IReadOnlyList<Media> mediaList, Source source, MediaActionContext ctx)
    {
        if (!Confirm(mediaList, source, ctx.Ui.Owner))
        {
            ctx.Logger.LogInformation("Пользователь отменил удаление");
            return Task.CompletedTask;
        }

        ctx.Logger.LogInformation("Запуск удаления {Count} медиа из '{Source}'",
            mediaList.Count, source.TitleFull);

        return BatchOperationRunner.RunAsync(mediaList,
            m => m.Title ?? string.Empty,
            async (m, ct) =>
            {
                await ctx.Orcestrator.DeleteMediaFromSourceAsync(m, source, ct);
                ctx.Logger.LogInformation("Удалено: '{Title}' из '{Source}'", m.Title, source.TitleFull);
            },
            "Удалено",
            "Пакетное удаление завершено с ошибками",
            ctx.Ui,
            ctx.Logger,
            ctx.ActionHolder,
            $"Удаление из {source.TitleFull} ({mediaList.Count})",
            ActionKind.Delete);
    }

    private static bool Confirm(IReadOnlyList<Media> mediaList, Source source, IWin32Window? owner)
    {
        if (mediaList.Count != 1)
        {
            return MessageBox.Show(owner,
                       $"Удалить {mediaList.Count} медиа из {source.TitleFull}?\n\nЭто действие нельзя отменить.",
                       "Подтверждение пакетного удаления",
                       MessageBoxButtons.YesNo,
                       MessageBoxIcon.Warning,
                       MessageBoxDefaultButton.Button2)
                   == DialogResult.Yes;
        }

        var media = mediaList[0];
        var isLastSource = media.Sources.Count(s => s.Status != MediaStatus.Skipped || !string.IsNullOrEmpty(s.ExternalId))
                           == 1;

        var message = isLastSource
            ? $"""
               Вы уверены, что хотите удалить медиа "{media.Title}" из {source.TitleFull}?

               ВНИМАНИЕ: Это последний источник для данного медиа. Запись будет полностью удалена из базы данных.
               """
            : $"""
               Вы уверены, что хотите удалить медиа "{media.Title}" из {source.TitleFull}?

               Медиа останется в других источниках.
               """;

        return MessageBox.Show(owner, message,
                   "Подтверждение удаления",
                   MessageBoxButtons.YesNo,
                   MessageBoxIcon.Warning,
                   MessageBoxDefaultButton.Button2)
               == DialogResult.Yes;
    }
}

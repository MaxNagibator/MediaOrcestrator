using MediaOrcestrator.Domain;
using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.Runner.MediaContextMenu.Actions;

internal sealed class SyncAction : IMediaMenuAction
{
    private const int BatchSyncMaxAttempts = 5;

    public int Order => 200;

    public IEnumerable<MenuItemSpec> Build(MediaSelection selection, MediaActionContext ctx)
    {
        foreach (var rel in ctx.Orcestrator.GetRelations())
        {
            if (selection.SpecificSource != null
                && rel.From.Id != selection.SpecificSource.Id
                && rel.To.Id != selection.SpecificSource.Id)
            {
                continue;
            }

            var eligible = selection.Items.Where(m => CanSync(m, rel)).ToList();
            var text = selection.IsBatch
                ? $"Синхронизировать {rel.From.TitleFull} → {rel.To.TitleFull} ({eligible.Count})"
                : $"Синхронизировать {rel.From.TitleFull} → {rel.To.TitleFull}";

            if (eligible.Count == 0)
            {
                yield return new(text, MenuIcons.Sync)
                {
                    Enabled = false,
                    Tooltip = selection.IsBatch
                        ? "Нет подходящих медиа для синхронизации"
                        : DescribeBlocker(selection.First, rel),
                };

                continue;
            }

            yield return new(text, MenuIcons.Sync)
            {
                Execute = () => ExecuteAsync(eligible, rel, ctx),
            };
        }
    }

    private static bool CanSync(Media media, SourceSyncRelation rel)
    {
        var from = media.Sources.FirstOrDefault(s => s.SourceId == rel.From.Id);
        var to = media.Sources.FirstOrDefault(s => s.SourceId == rel.To.Id);

        return from is { Status: MediaStatus.Ok }
               && to is not { Status: MediaStatus.Ok }
               && to is not { Status: MediaStatus.Skipped };
    }

    private static string DescribeBlocker(Media media, SourceSyncRelation rel)
    {
        var from = media.Sources.FirstOrDefault(s => s.SourceId == rel.From.Id);
        var to = media.Sources.FirstOrDefault(s => s.SourceId == rel.To.Id);

        if (from is { Status: MediaStatus.Skipped })
        {
            return "Исходное хранилище помечено как пропущенное";
        }

        if (to is { Status: MediaStatus.Skipped })
        {
            return "Целевое хранилище помечено как пропущенное";
        }

        if (from == null && to == null)
        {
            return "Медиа отсутствует в исходном хранилище";
        }

        if (from == null)
        {
            return "Исходное хранилище недоступно";
        }

        if (from.Status != MediaStatus.Ok)
        {
            var statusText = MediaStatusHelper.GetById(from.Status).Text;
            return $"Медиа в исходном хранилище имеет статус «{statusText}»";
        }

        if (to is { Status: MediaStatus.Ok })
        {
            return "Медиа уже существует в целевом хранилище";
        }

        return "Синхронизация недоступна";
    }

    private static async Task ExecuteAsync(IReadOnlyList<Media> mediaList, SourceSyncRelation rel, MediaActionContext ctx)
    {
        if (mediaList.Count > 1)
        {
            var confirm = MessageBox.Show(ctx.Ui.Owner,
                $"Синхронизировать {mediaList.Count} медиа из {rel.From.TitleFull} в {rel.To.TitleFull}?",
                "Подтверждение пакетной синхронизации",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
            {
                return;
            }
        }

        ctx.Logger.LogInformation("Запуск синхронизации {Count} медиа: {From} → {To}",
            mediaList.Count, rel.From.TitleFull, rel.To.TitleFull);

        await BatchOperationRunner.RunAsync(mediaList,
            m => m.Title ?? string.Empty,
            async (m, ct) =>
            {
                await ctx.RetryRunner.RunAsync(m, rel, maxAttempts: BatchSyncMaxAttempts, cancellationToken: ct);
                ctx.Logger.LogInformation("Синхронизировано: '{Title}'", m.Title);
            },
            "Синхронизировано",
            "Пакетная синхронизация завершена с ошибками",
            ctx.Ui,
            ctx.Logger,
            ctx.ActionHolder,
            $"Синхронизация: {rel.From.TitleFull} → {rel.To.TitleFull}",
            ActionKind.Sync);
    }
}

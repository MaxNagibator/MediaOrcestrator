using MediaOrcestrator.Domain;
using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.Runner.MediaContextMenu.Actions;

internal sealed class RemoveFromDatabaseAction : IMediaMenuAction
{
    public int Order => 710;

    public IEnumerable<MenuItemSpec> Build(MediaSelection selection, MediaActionContext ctx)
    {
        if (selection.Items.Count == 0)
        {
            yield break;
        }

        var text = selection.IsBatch
            ? $"Удалить из базы оркестратора ({selection.Items.Count})"
            : "Удалить из базы оркестратора";

        yield return new(text, MenuIcons.Delete)
        {
            Execute = () => ExecuteAsync(selection.Items, ctx),
        };
    }

    private static Task ExecuteAsync(IReadOnlyList<Media> mediaList, MediaActionContext ctx)
    {
        var subject = mediaList.Count == 1
            ? $"«{mediaList[0].Title}»"
            : $"{mediaList.Count} медиа";

        var confirmed = MessageBox.Show(ctx.Ui.Owner,
                            $"""
                             Убрать {subject} из базы оркестратора?

                             На площадках ничего не удалится. Если медиа снова найдётся при сканировании, оно вернётся в список.
                             """,
                            "Удаление из базы",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning,
                            MessageBoxDefaultButton.Button2)
                        == DialogResult.Yes;

        if (!confirmed)
        {
            ctx.Logger.LogInformation("Пользователь отменил удаление из базы");
            return Task.CompletedTask;
        }

        return BatchOperationRunner.RunAsync(mediaList,
            m => m.Title ?? string.Empty,
            (m, _) =>
            {
                ctx.Orcestrator.RemoveMedia(m);
                ctx.Logger.LogInformation("Медиа '{Title}' удалено из базы оркестратора", m.Title);
                return Task.CompletedTask;
            },
            "Удалено из базы",
            "Удаление из базы завершено с ошибками",
            ctx.Ui,
            ctx.Logger,
            ctx.ActionHolder,
            $"Удаление из базы ({mediaList.Count})",
            ActionKind.Delete);
    }
}

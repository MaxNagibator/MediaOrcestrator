using MediaOrcestrator.Domain;
using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.Runner.MediaContextMenu.Actions;

internal sealed class ConvertAction : IAsyncMediaMenuAction
{
    public int Order => 800;

    public string LoadingPlaceholder => "Загрузка конвертаций...";

    public Bitmap? LoadingIcon => MenuIcons.Convert;

    public IEnumerable<MenuItemSpec> Build(MediaSelection selection, MediaActionContext ctx)
    {
        return [];
    }

    public async Task<IEnumerable<MenuItemSpec>> BuildAsync(MediaSelection selection, MediaActionContext ctx, CancellationToken ct)
    {
        var allSources = ctx.Orcestrator.GetSources();

        var sources = selection.SpecificSource != null
            ? [selection.SpecificSource]
            : allSources.Where(s => !s.IsDisable).ToList();

        var result = new List<MenuItemSpec>();

        foreach (var source in sources)
        {
            ct.ThrowIfCancellationRequested();

            if (source.Type == null)
            {
                continue;
            }

            var convertTypes = source.Type.GetAvailableConvertTypes();
            if (convertTypes.Length == 0)
            {
                continue;
            }

            var sourceItems = selection.Items
                .Where(m => m.Sources.Any(s => s.SourceId == source.Id
                                               && s.Status != MediaStatus.Skipped
                                               && !string.IsNullOrEmpty(s.ExternalId)))
                .ToList();

            if (sourceItems.Count == 0)
            {
                continue;
            }

            var dtos = new Dictionary<Media, MediaDto?>();
            foreach (var media in sourceItems)
            {
                ct.ThrowIfCancellationRequested();

                var link = media.Sources.First(s => s.SourceId == source.Id);
                try
                {
                    dtos[media] = await source.Type.GetMediaByIdAsync(link.ExternalId, source.Settings);
                }
                catch (Exception ex)
                {
                    ctx.Logger.LogWarning(ex, "Не удалось загрузить метаданные для {Source}: {ExternalId}",
                        source.TitleFull, link.ExternalId);

                    dtos[media] = null;
                }
            }

            foreach (var t in convertTypes)
            {
                var eligible = sourceItems
                    .Where(m => dtos.TryGetValue(m, out var dto)
                                && dto != null
                                && source.Type.CheckConvertAvailability(t.Id, dto).IsAvailable)
                    .ToList();

                var text = selection.IsBatch
                    ? $"Конвертировать {t.Name} ({source.TitleFull}) ({eligible.Count})"
                    : $"Конвертировать {t.Name} ({source.TitleFull})";

                if (eligible.Count == 0)
                {
                    var firstReason = dtos.Values.FirstOrDefault(d => d != null) is { } anyDto
                        ? source.Type.CheckConvertAvailability(t.Id, anyDto).Reason
                        : "Не удалось получить метаданные";

                    result.Add(new(text, MenuIcons.Convert)
                    {
                        Enabled = false,
                        Tooltip = selection.IsBatch
                            ? "Нет подходящих медиа для конвертации"
                            : firstReason ?? "Конвертация недоступна",
                    });

                    continue;
                }

                var convertType = t;
                result.Add(new(text, MenuIcons.Convert)
                {
                    Execute = () => ExecuteAsync(eligible, source, convertType, ctx),
                });
            }
        }

        return result;
    }

    private static async Task ExecuteAsync(IReadOnlyList<Media> mediaList, Source source, ConvertType convertType, MediaActionContext ctx)
    {
        var cts = new CancellationTokenSource();
        ctx.Ui.RegisterConvertCancellation(cts);

        var total = mediaList.Count;
        var actionName = total > 1
            ? $"Конвертация {convertType.Name} ({source.TitleFull}, {total})"
            : $"Конвертация {convertType.Name}: «{mediaList[0].Title}»";

        var running = ctx.ActionHolder.Register(actionName, "В процессе", total, cts, kind: ActionKind.Convert);
        using var scope = ctx.ActionHolder.BeginScope(running);

        var errors = new List<(Media media, Exception ex)>();
        var converted = 0;

        ctx.Logger.LogInformation("Запуск конвертации {Name} ({Source}) для {Count} медиа",
            convertType.Name, source.TitleFull, total);

        try
        {
            for (var i = 0; i < total; i++)
            {
                cts.Token.ThrowIfCancellationRequested();

                var media = mediaList[i];
                var link = media.Sources.First(s => s.SourceId == source.Id);
                var index = i;

                using var itemCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token);
                var itemAction = ctx.ActionHolder.Register($"Конвертация {convertType.Name}: «{media.Title}»",
                    "Подготовка...",
                    100,
                    itemCts,
                    kind: ActionKind.Convert);

                var eta = new SubtitleEtaTicker(itemAction, new());

                var progress = new Progress<ConvertProgress>(p =>
                {
                    eta.Report(p.Percent);
                    itemAction.SetProgress((int)Math.Clamp(p.Percent, 0, 100));
                    itemAction.Status = $"Конвертация {p.Percent:F0}%";
                    ctx.Ui.ShowConvertProgress(p.Percent, BuildProgressText(convertType, media, index + 1, total, p.Percent));
                });

                ctx.Ui.ShowConvertProgress(0, BuildProgressText(convertType, media, i + 1, total, null));

                try
                {
                    await source.Type.ConvertAsync(convertType.Id, link.ExternalId, source.Settings, progress, itemCts.Token);
                    await ctx.Orcestrator.ForceUpdateMetadataAsync(media, source.Id);
                    itemAction.Subtitle = string.Empty;
                    itemAction.Finish("Готово");
                    converted++;
                    running.ProgressPlus();
                }
                catch (OperationCanceledException)
                {
                    itemAction.Subtitle = string.Empty;
                    itemAction.MarkCancelled();
                    throw;
                }
                catch (Exception ex)
                {
                    ctx.Logger.LogError(ex, "Ошибка конвертации {Name}: {ExternalId}", convertType.Name, link.ExternalId);
                    itemAction.Subtitle = string.Empty;
                    itemAction.Fail("Ошибка: " + ex.Message, ex);
                    errors.Add((media, ex));
                    running.ProgressPlus();
                }
            }

            ShowErrors("Конвертировано", "Конвертация завершена с ошибками", converted, total, errors, ctx.Ui.Owner);
        }
        catch (OperationCanceledException)
        {
            ctx.Logger.LogInformation("Конвертация отменена пользователем");
        }
        finally
        {
            if (cts.IsCancellationRequested)
            {
                running.MarkCancelled($"Отменено: {converted} из {total}");
            }
            else if (errors.Count > 0)
            {
                var details = string.Join(Environment.NewLine, errors.Select(e => $"- {e.media.Title}: {e.ex.Message}"));
                var message = $"Ошибок: {errors.Count} из {total}";
                running.Fail(message, new AggregateException(message + Environment.NewLine + details,
                    errors.Select(e => e.ex)));
            }
            else
            {
                running.Finish($"Готово: {converted} из {total}");
            }

            ctx.Ui.HideConvertProgress();
            ctx.Ui.NotifyDataChanged();
            ctx.Ui.RegisterConvertCancellation(null);
            cts.Dispose();
        }
    }

    private static string BuildProgressText(ConvertType convertType, Media media, int currentIndex, int total, double? percent)
    {
        var prefix = total > 1
            ? $"{convertType.Name} [{currentIndex}/{total}]: {media.Title}"
            : $"{convertType.Name}: {media.Title}";

        return percent is { } p
            ? $"{prefix} — {p:F0}%"
            : $"{prefix}...";
    }

    private static void ShowErrors(string bodyPrefix, string title, int succeeded, int total, List<(Media media, Exception ex)> errors, IWin32Window? owner)
    {
        if (errors.Count == 0)
        {
            return;
        }

        var details = string.Join("\n", errors.Select(e => $"- {e.media.Title}: {e.ex.Message}"));

        MessageBox.Show(owner,
            $"""
             {bodyPrefix}: {succeeded} из {total}

             Ошибки:
             {details}
             """,
            title,
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }
}

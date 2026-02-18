using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.Domain;

public class SyncPlanner(ILogger<SyncPlanner> logger)
{
    public List<SyncIntent> Plan(List<Media> medias, List<SourceSyncRelation> relations)
    {
        logger.LogInformation("Начало планирования синхронизации для {MediaCount} медиа и {RelationCount} связей.",
            medias.Count, relations.Count);

        var rootIntents = new List<SyncIntent>();

        var activeRelations = relations.Where(r => !r.IsDisable).ToList();
        foreach (var media in medias)
        {
            foreach (var relation in activeRelations)
            {
                if (!NeedsSync(media, relation))
                {
                    continue;
                }

                var isContinuation = activeRelations.Any(r => r.ToId == relation.FromId && NeedsSync(media, r));
                if (isContinuation)
                {
                    continue;
                }

                logger.LogDebug("Найдена корневая точка синхронизации для '{MediaTitle}': {From} -> {To}",
                    media.Title, relation.From.TypeId, relation.To.TypeId);

                var intent = CreateIntent(media, relation, activeRelations);
                rootIntents.Add(intent);
            }
        }

        logger.LogInformation("Планирование завершено. Создано {RootIntentCount} корневых намерений.", rootIntents.Count);
        return rootIntents;
    }

    private bool NeedsSync(Media media, SourceSyncRelation relation)
    {
        var fromSource = media.Sources.FirstOrDefault(x => x.SourceId == relation.FromId);
        var toSource = media.Sources.FirstOrDefault(x => x.SourceId == relation.ToId);

        return fromSource != null && toSource == null;
    }

    private SyncIntent CreateIntent(Media media, SourceSyncRelation relation, List<SourceSyncRelation> allRelations)
    {
        var intent = new SyncIntent
        {
            Media = media,
            From = relation.From,
            To = relation.To,
            Relation = relation,
        };

        var nextRelations = allRelations
            .Where(r => r.FromId == relation.ToId)
            .ToList();

        foreach (var nextRel in nextRelations)
        {
            var toSource = media.Sources.FirstOrDefault(x => x.SourceId == nextRel.ToId);
            if (toSource != null)
            {
                continue;
            }

            logger.LogTrace("Добавление следующего шага в цепочку для '{MediaTitle}': {From} -> {To}",
                media.Title, nextRel.From.TypeId, nextRel.To.TypeId);

            intent.NextIntents.Add(CreateIntent(media, nextRel, allRelations));
        }

        return intent;
    }
}

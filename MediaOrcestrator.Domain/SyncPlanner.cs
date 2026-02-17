using Microsoft.Extensions.Logging;

namespace MediaOrcestrator.Domain;

// TODO: Убрать комментарии после обкатки и утверждения механизма
public class SyncPlanner(ILogger<SyncPlanner> logger)
{
    public List<SyncIntent> Plan(List<Media> medias, List<SourceSyncRelation> relations)
    {
        logger.LogInformation("Начало планирования синхронизации для {MediaCount} медиа и {RelationCount} связей.", 
            medias.Count, relations.Count);

        var rootIntents = new List<SyncIntent>();

        // Нам нужно найти «точки входа» для каждого медиа-ресурса.
        // Один медиа-ресурс может потребовать синхронизации через несколько цепочек связей.
        foreach (var media in medias)
        {
            var activeRelations = relations.Where(r => !r.IsDisable).ToList();
            foreach (var relation in activeRelations)
            {
                // Проверяем, является ли эта связь потенциальной «корневой» синхронизацией для данного медиа.
                // Связь считается корневой, если синхронизация НЕОБХОДИМА и она не является продолжением другой нужной синхронизации.
                if (!NeedsSync(media, relation))
                {
                    continue;
                }

                // Есть ли какая-либо связь, ведущая К источнику relation.From?
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
            // Мы предполагаем, что после выполнения текущего намерения (intent), медиа БУДЕТ находиться в relation.To
            // Поэтому проверяем, что его еще НЕТ в источнику nextRel.To
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

namespace MediaOrcestrator.Runner;

/// <summary>
/// Описание столбца метаданных в матрице. Учитывает принадлежность к источнику.
/// </summary>
/// <param name="ColumnId">Уникальный идентификатор столбца (для сохранения в настройках).</param>
/// <param name="Key">Оригинальный ключ метаданных.</param>
/// <param name="SourceId">ID источника (null если ключ уникален для одного источника).</param>
/// <param name="DisplayName">Отображаемое имя в заголовке столбца и фильтре.</param>
/// <param name="DisplayType">Тип отображения для форматирования.</param>
public record MetadataColumnInfo(string ColumnId, string Key, string? SourceId, string DisplayName, string? DisplayType);

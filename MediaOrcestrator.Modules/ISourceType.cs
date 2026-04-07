namespace MediaOrcestrator.Modules;

/// <summary>
/// Контракт плагина-источника медиа.
/// </summary>
public interface ISourceType
{
    /// <summary>
    /// Уникальный строковый идентификатор плагина.
    /// </summary>
    /// <remarks>
    /// Используется для привязки <see cref="Source.TypeId" /> → экземпляр плагина.
    /// </remarks>
    string Name { get; }

    /// <summary>
    /// Направление синхронизации, которое поддерживает источник.
    /// </summary>
    SyncDirection ChannelType { get; }

    /// <summary>
    /// Описание настроек, специфичных для источника.
    /// </summary>
    IEnumerable<SourceSettings> SettingsKeys { get; }

    /// <summary>
    /// Возвращает допустимые варианты для настройки типа <see cref="SettingType.Dropdown" />.
    /// </summary>
    /// <param name="settingKey">Ключ настройки, для которой запрашиваются варианты.</param>
    /// <param name="currentSettings">Текущие значения всех настроек источника (могут влиять на список вариантов).</param>
    /// <returns>Список вариантов для выпадающего списка; пустой, если варианты недоступны.</returns>
    Task<List<SettingOption>> GetSettingOptionsAsync(string settingKey, Dictionary<string, string> currentSettings)
    {
        return Task.FromResult(new List<SettingOption>());
    }

    /// <summary>
    /// Формирует ссылку на медиа во внешнем сервисе.
    /// </summary>
    /// <param name="externalId">Идентификатор медиа в источнике.</param>
    /// <param name="settings">Конфигурация источника.</param>
    /// <returns>URI или <see langword="null" />, если источник не предоставляет внешних ссылок.</returns>
    Uri? GetExternalUri(string externalId, Dictionary<string, string> settings)
    {
        return null;
    }

    /// <summary>
    /// Возвращает типы конвертации, которые поддерживает источник.
    /// </summary>
    /// <returns>Массив доступных типов; пустой – конвертация не поддерживается.</returns>
    ConvertType[] GetAvailableConvertTypes()
    {
        return [];
    }

    /// <summary>
    /// Проверяет, применим ли конкретный тип конвертации к данному медиа.
    /// </summary>
    /// <param name="typeId">Идентификатор типа конвертации из <see cref="GetAvailableConvertTypes" />.</param>
    /// <param name="media">Медиа, для которого проверяется доступность.</param>
    /// <returns>Доступность и причина в случае недоступности.</returns>
    ConvertAvailability CheckConvertAvailability(int typeId, MediaDto media)
    {
        return new(false, "Конвертация не поддерживается");
    }

    /// <summary>
    /// Выполняет конвертацию медиа указанного типа.
    /// </summary>
    /// <param name="typeId">Идентификатор типа конвертации из <see cref="GetAvailableConvertTypes" />.</param>
    /// <param name="externalId">Идентификатор медиа в источнике.</param>
    /// <param name="settings">Конфигурация источника.</param>
    /// <param name="progress">Обратная связь о прогрессе; <see langword="null" /> если не требуется.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <exception cref="NotImplementedException">Источник не поддерживает конвертацию (реализация по умолчанию).</exception>
    Task ConvertAsync(int typeId, string externalId, Dictionary<string, string> settings, IProgress<ConvertProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        // TODO: Возможно лучше бросать not supported
        throw new NotImplementedException();
    }

    /// <summary>
    /// Потоковое перечисление всех медиа из источника.
    /// </summary>
    /// <param name="settings">Конфигурация источника.</param>
    /// <param name="isFull">
    /// <see langword="true" /> – полный обход всех медиа;
    /// <see langword="false" /> – инкрементальный (только новые).
    /// </param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Асинхронная последовательность медиа, отсортированная от самых свежих к старым.</returns>
    IAsyncEnumerable<MediaDto> GetMedia(Dictionary<string, string> settings, bool isFull, CancellationToken cancellationToken = default);

    /// <summary>
    /// Получает метаданные конкретного медиа по идентификатору в источнике.
    /// </summary>
    /// <param name="externalId">Идентификатор медиа в источнике.</param>
    /// <param name="settings">Конфигурация источника.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Метаданные медиа или <see langword="null" />, если медиа не найдено.</returns>
    Task<MediaDto?> GetMediaByIdAsync(string externalId, Dictionary<string, string> settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Скачивает медиа из источника во временный файл.
    /// </summary>
    /// <param name="videoId">Идентификатор медиа в источнике.</param>
    /// <param name="settings">Конфигурация источника.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>
    /// <see cref="MediaDto" /> с заполнённым <see cref="MediaDto.TempDataPath" />,
    /// готовый для передачи в <see cref="UploadAsync" />.
    /// </returns>
    /// <exception cref="InvalidOperationException">Медиа не найдено по указанному идентификатору.</exception>
    Task<MediaDto> DownloadAsync(string videoId, Dictionary<string, string> settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Загружает новое медиа в источник.
    /// </summary>
    /// <param name="media">Метаданные и данные медиа для загрузки (включая <see cref="MediaDto.TempDataPath" />).</param>
    /// <param name="settings">Конфигурация источника.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns><see cref="UploadResult" /> с идентификатором созданного элемента и статусом операции.</returns>
    Task<UploadResult> UploadAsync(MediaDto media, Dictionary<string, string> settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Обновляет метаданные уже существующего медиа в источнике.
    /// </summary>
    /// <param name="externalId">Идентификатор обновляемого медиа в источнике.</param>
    /// <param name="tempMedia">Новые метаданные.</param>
    /// <param name="settings">Конфигурация источника.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns><see cref="UploadResult" /> со статусом операции.</returns>
    Task<UploadResult> UpdateAsync(string externalId, MediaDto tempMedia, Dictionary<string, string> settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Удаляет медиа из источника.
    /// </summary>
    /// <param name="externalId">Идентификатор медиа в источнике.</param>
    /// <param name="settings">Конфигурация источника.</param>
    /// <param name="cancellationToken">Токен отмены.</param>
    Task DeleteAsync(string externalId, Dictionary<string, string> settings, CancellationToken cancellationToken = default);
}

/// <summary>
/// Абстракция UI-взаимодействия при аутентификации плагина.
/// </summary>
public interface IAuthUI
{
    /// <summary>
    /// Запрашивает текстовый ввод у пользователя.
    /// </summary>
    /// <param name="prompt">Текст подсказки, отображаемый в диалоге.</param>
    /// <param name="isPassword">Маскировать ли вводимые символы.</param>
    /// <returns>Введённая строка или <see langword="null" />, если пользователь отменил ввод.</returns>
    Task<string?> PromptInputAsync(string prompt, bool isPassword = false);

    /// <summary>
    /// Открывает браузер для OAuth/cookie-авторизации на внешнем сервисе.
    /// </summary>
    /// <param name="url">URL страницы авторизации.</param>
    /// <param name="existingStatePath">
    /// Путь к сохранённому состоянию сессии; <see langword="null" /> – начать с чистого листа.
    /// </param>
    /// <returns>Путь к файлу с сохранённым состоянием или <see langword="null" /> при отмене.</returns>
    Task<string?> OpenBrowserAsync(string url, string? existingStatePath = null);

    /// <summary>
    /// Показывает информационное сообщение пользователю.
    /// </summary>
    /// <param name="message">Текст сообщения.</param>
    Task ShowMessageAsync(string message);
}

/// <summary>
/// Поддержка аутентификации для плагина-источника.
/// </summary>
/// <remarks>
/// Плагин реализует этот интерфейс дополнительно к <see cref="ISourceType" />,
/// если для работы требуется авторизация.
/// </remarks>
public interface IAuthenticatable
{
    /// <summary>
    /// Проверяет, авторизован ли источник с текущими настройками.
    /// </summary>
    /// <param name="settings">Конфигурация источника.</param>
    /// <returns><see langword="true" />, если авторизация выполнена и валидна.</returns>
    bool IsAuthenticated(Dictionary<string, string> settings);

    /// <summary>
    /// Запускает интерактивный процесс авторизации.
    /// </summary>
    /// <param name="settings">Конфигурация источника; может быть дополнена токенами/cookie в процессе.</param>
    /// <param name="ui">Провайдер UI-взаимодействия с пользователем.</param>
    /// <param name="ct">Токен отмены.</param>
    Task AuthenticateAsync(Dictionary<string, string> settings, IAuthUI ui, CancellationToken ct);
}

/// <summary>
/// Тип конвертации, доступный для источника.
/// </summary>
public sealed class ConvertType
{
    /// <summary>
    /// Числовой идентификатор типа конвертации.
    /// </summary>
    /// <seealso cref="ISourceType.ConvertAsync" />
    /// <seealso cref="ISourceType.CheckConvertAvailability" />
    public int Id { get; set; }

    /// <summary>
    /// Отображаемое название типа конвертации.
    /// </summary>
    public required string Name { get; set; }
}

/// <summary>
/// Результат проверки доступности конвертации для конкретного медиа.
/// </summary>
/// <param name="IsAvailable">Доступна ли конвертация.</param>
/// <param name="Reason">Причина недоступности; <see langword="null" /> если доступна.</param>
public sealed record ConvertAvailability(bool IsAvailable, string? Reason);

/// <summary>
/// Прогресс выполнения конвертации.
/// </summary>
/// <param name="Percent">Процент выполнения (0–100).</param>
/// <param name="FileName">Имя обрабатываемого файла.</param>
public sealed record ConvertProgress(double Percent, string FileName);

/// <summary>
/// Тип элемента управления для настройки источника.
/// </summary>
public enum SettingType
{
    /// <summary>Не задан.</summary>
    None = 0,

    /// <summary>Свободный текстовый ввод.</summary>
    Text = 1,

    /// <summary>
    /// Выпадающий список. Варианты задаются статически через <see cref="SourceSettings.Options" />
    /// или загружаются динамически через <see cref="ISourceType.GetSettingOptionsAsync" />.
    /// </summary>
    Dropdown = 2,
}

/// <summary>
/// Результат операции загрузки или обновления медиа в источнике.
/// </summary>
public sealed class UploadResult
{
    /// <summary>Статус операции.</summary>
    public MediaStatus Status { get; set; }

    /// <summary>Дополнительное сообщение (причина частичного успеха, текст ошибки и т.д.).</summary>
    public string? Message { get; set; }

    /// <summary>
    /// Внешний идентификатор созданного/обновлённого медиа в источнике.
    /// <see langword="null" /> – элемент не был создан.
    /// </summary>
    public string? Id { get; set; }
}

/// <summary>
/// Описание одной настройки источника.
/// </summary>
/// <seealso cref="ISourceType.SettingsKeys" />
public sealed class SourceSettings
{
    /// <summary>Ключ в словаре <c>settings</c>.</summary>
    public required string Key { get; set; }

    /// <summary>Заголовок настройки.</summary>
    public required string Title { get; set; }

    /// <summary>Обязательна ли настройка для работы источника.</summary>
    public bool IsRequired { get; set; }

    /// <summary>Значение по умолчанию.</summary>
    public string? DefaultValue { get; set; }

    /// <summary>Поясняющий текст.</summary>
    public string? Description { get; set; }

    /// <summary>Тип элемента управления. По умолчанию – <see cref="SettingType.Text" />.</summary>
    public SettingType Type { get; set; } = SettingType.Text;

    /// <summary>
    /// Статический список вариантов для <see cref="SettingType.Dropdown" />.
    /// Если <see langword="null" /> – варианты загружаются через
    /// <see cref="ISourceType.GetSettingOptionsAsync" />.
    /// </summary>
    public List<SettingOption>? Options { get; set; }
}

/// <summary>
/// Вариант значения для настройки типа <see cref="SettingType.Dropdown" />.
/// </summary>
public sealed class SettingOption
{
    /// <summary>Значение, сохраняемое в словарь <c>settings</c>.</summary>
    public required string Value { get; set; }

    /// <summary>Отображаемый текст.</summary>
    public required string Label { get; set; }
}

/// <summary>
/// Направление синхронизации, поддерживаемое источником.
/// </summary>
public enum SyncDirection
{
    /// <summary>Не задано.</summary>
    None = 0,

    /// <summary>Только получение медиа из источника.</summary>
    OnlyDownload = 1,

    /// <summary>Только загрузка медиа в источник.</summary>
    OnlyUpload = 2,

    /// <summary>Полная двусторонняя синхронизация.</summary>
    Full = 3,
}

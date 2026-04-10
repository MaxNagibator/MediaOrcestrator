namespace MediaOrcestrator.Modules;

/// <summary>
/// Исключение, сигнализирующее о детерминированной ошибке: повторять операцию бессмысленно.
/// Ретрай-циклы должны прекращать попытки при его появлении.
/// </summary>
/// <remarks>
/// Примеры уместного использования:
/// <list type="bullet">
///     <item>Не удалось определить кодек видео (ffprobe недоступен или формат неопознан).</item>
///     <item>HTTP 4xx от внешнего API (401/403/400) - повтор не решит проблему.</item>
///     <item>Файл не найден на момент загрузки.</item>
/// </list>
/// Для транзиентных ошибок (5xx, таймауты, сеть) использовать обычные исключения - они ретраятся.
/// </remarks>
public sealed class NonRetriableException : Exception
{
    public NonRetriableException(string message)
        : base(message)
    {
    }

    public NonRetriableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

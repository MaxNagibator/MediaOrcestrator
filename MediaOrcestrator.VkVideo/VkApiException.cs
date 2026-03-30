namespace MediaOrcestrator.VkVideo;

// TODO: Думал красиво обернуть, но забил
public sealed class VkApiException(int errorCode, string errorMessage, string method)
    : Exception($"Ошибка VK API {method}: [{errorCode}] {errorMessage}")
{
    public int ErrorCode { get; } = errorCode;
    public string Method { get; } = method;
}

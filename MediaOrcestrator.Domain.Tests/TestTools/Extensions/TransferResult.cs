using MediaOrcestrator.Modules;

namespace MediaOrcestrator.Domain.Tests.TestTools.Extensions;

public sealed class TransferResult(ISourceType fromType, ISourceType toType, Exception? error)
{
    public ISourceType FromType { get; } = fromType;
    public ISourceType ToType { get; } = toType;
    public Exception? Error { get; } = error;
}

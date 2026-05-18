using MediaOrcestrator.Modules;
using NSubstitute;

namespace MediaOrcestrator.Domain.Tests.TestTools.Extensions;

public static class TransferAssertions
{
    public static async Task<TransferResult> ShouldNotReupload(this Task<TransferResult> task)
    {
        var result = await task;

        Assert.That(result.Error, Is.Null, "Трансфер завершился ошибкой, а ожидался безшумный пропуск");

        await result.FromType.DidNotReceive()
            .DownloadAsync(Arg.Any<string>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<IProgress<DownloadProgress>>(), Arg.Any<CancellationToken>());

        await result.ToType.DidNotReceive()
            .UploadAsync(Arg.Any<MediaDto>(), Arg.Any<Dictionary<string, string>>(), Arg.Any<IProgress<UploadProgress>>(), Arg.Any<CancellationToken>());

        return result;
    }

    public static async Task<TransferResult> ShouldFailWith(this Task<TransferResult> task, string message)
    {
        var result = await task;

        Assert.That(result.Error, Is.TypeOf<InvalidOperationException>());
        Assert.That(result.Error!.Message, Is.EqualTo(message));

        return result;
    }
}

using YoutubeExplode.Channels;

namespace MediaOrcestrator.Youtube;

public static class ChannelUrlResolver
{
    public static async Task<T?> ResolveAsync<T>(
        string channelUrl,
        Func<string, Task<T?>> byId,
        Func<string, Task<T?>> bySlug,
        Func<string, Task<T?>> byHandle,
        Func<string, Task<T?>> byUserName) where T : class
    {
        if (ChannelId.TryParse(channelUrl) is { } id)
        {
            var result = await byId(id.Value);

            if (result is not null)
            {
                return result;
            }
        }

        if (ChannelSlug.TryParse(channelUrl) is { } slug)
        {
            var result = await bySlug(slug.Value);

            if (result is not null)
            {
                return result;
            }
        }

        if (ChannelHandle.TryParse(channelUrl) is { } handle)
        {
            var result = await byHandle(handle.Value);

            if (result is not null)
            {
                return result;
            }
        }

        if (UserName.TryParse(channelUrl) is { } userName)
        {
            var result = await byUserName(userName.Value);

            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }
}

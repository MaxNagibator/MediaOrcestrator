using MediaOrcestrator.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MediaOrcestrator.Instagram;

public sealed class InstagramModule : IPluginModule
{
    public void Register(IServiceCollection services)
    {
        services.AddOptions<InstagramOptions>();
        services.AddHttpClient<InstagramService>((sp, client) =>
        {
            client.Timeout = sp.GetRequiredService<IOptions<InstagramOptions>>().Value.UploadTimeout;
        });
        services.AddSingleton<ISourceType, InstagramChannel>();
    }
}

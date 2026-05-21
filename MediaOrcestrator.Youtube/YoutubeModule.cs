using MediaOrcestrator.Modules;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Timeout;
using System.Net;

namespace MediaOrcestrator.Youtube;

public sealed class YoutubeModule : IPluginModule
{
    public void Register(IServiceCollection services)
    {
        services.AddOptions<YoutubeOptions>();

        services
            .AddHttpClient(YoutubeServiceFactory.ApiClientName, ConfigureApiClient)
            .ConfigurePrimaryHttpMessageHandler(CreateHandler)
            .AddResilienceHandler("youtube-api", BuildResiliencePipeline);

        services.AddSingleton<IYoutubeServiceFactory, YoutubeServiceFactory>();
        services.AddSingleton<YoutubeExplodeReadService>();
        services.AddSingleton<YoutubeYtDlpReadService>();
        services.AddSingleton<YoutubeApiReadService>();
        services.AddSingleton<YoutubeCommentsReadService>();
        services.AddSingleton<YoutubeYtDlpCommentsReadService>();
        services.AddSingleton<YoutubeUploadService>();
        services.AddSingleton<ISourceType, YoutubeChannel>();
    }

    private static void ConfigureApiClient(
        IServiceProvider sp,
        HttpClient client)
    {
        client.Timeout = Timeout.InfiniteTimeSpan;
    }

    private static SocketsHttpHandler CreateHandler(IServiceProvider sp)
    {
        var options = sp.GetRequiredService<IOptions<YoutubeOptions>>().Value;

        return new()
        {
            UseCookies = false,
            PooledConnectionLifetime = options.PooledConnectionLifetime,
            PooledConnectionIdleTimeout = options.PooledConnectionIdleTimeout,
        };
    }

    private static void BuildResiliencePipeline(
        ResiliencePipelineBuilder<HttpResponseMessage> builder,
        ResilienceHandlerContext context)
    {
        var options = context.ServiceProvider.GetRequiredService<IOptions<YoutubeOptions>>().Value;

        builder
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = options.ApiTotalTimeout,
                Name = "youtube-api-total-timeout",
            })
            .AddRetry(new()
            {
                Name = "youtube-api-retry",
                MaxRetryAttempts = options.RetryCount,
                Delay = options.RetryBaseDelay,
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = args => ValueTask.FromResult(IsTransientFailure(args.Outcome)),
            })
            .AddCircuitBreaker(new()
            {
                Name = "youtube-api-circuit-breaker",
                MinimumThroughput = options.CircuitBreakerMinimumThroughput,
                FailureRatio = options.CircuitBreakerFailureRatio,
                SamplingDuration = options.CircuitBreakerSamplingDuration,
                ShouldHandle = args => ValueTask.FromResult(IsTransientFailure(args.Outcome)),
            })
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = options.ApiAttemptTimeout,
                Name = "youtube-api-attempt-timeout",
            });
    }

    private static bool IsTransientFailure(Outcome<HttpResponseMessage> outcome)
    {
        if (outcome.Exception is HttpRequestException or TimeoutRejectedException)
        {
            return true;
        }

        if (outcome.Result is null)
        {
            return false;
        }

        var status = outcome.Result.StatusCode;

        return status == HttpStatusCode.RequestTimeout
               || status == HttpStatusCode.TooManyRequests
               || (int)status >= 500;
    }
}

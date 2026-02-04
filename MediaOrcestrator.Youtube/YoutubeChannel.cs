using MediaOrcestrator.Core;
using MediaOrcestrator.Modules;
using Microsoft.Extensions.Logging;
using YoutubeExplode;
using YoutubeExplode.Channels;
using YoutubeExplode.Videos.Streams;

namespace MediaOrcestrator.Youtube;

public class YoutubeChannel(ILogger<YoutubeChannel> logger) : ISourceType
{
    private readonly Func<YoutubeClient, string, Task<Channel?>>[] _parsers =
    [
        async (youtubeClient, url) => ChannelId.TryParse(url) is { } id ? await youtubeClient.Channels.GetAsync(id) : null,
        async (youtubeClient, url) => ChannelSlug.TryParse(url) is { } slug ? await youtubeClient.Channels.GetBySlugAsync(slug) : null,
        async (youtubeClient, url) => ChannelHandle.TryParse(url) is { } handle ? await youtubeClient.Channels.GetByHandleAsync(handle) : null,
        async (youtubeClient, url) => UserName.TryParse(url) is { } userName ? await youtubeClient.Channels.GetByUserAsync(userName) : null,
    ];

    public SyncDirection ChannelType => SyncDirection.OnlyUpload;

    public string Name => "Youtube";

    public IEnumerable<SourceSettings> SettingsKeys { get; } =
    [
        new()
        {
            Key = "channel_id",
            IsRequired = true,
            Title = "идентификатор канала",
        },
    ];

    public async IAsyncEnumerable<MediaDto> GetMedia(Dictionary<string, string> settings)
    {
        //var channelUrl = "https://www.youtube.com/@bobito217";

        var channelUrl = settings["channel_id"];
        logger.LogInformation("Получение медиа для канала: {ChannelUrl}", channelUrl);

        var youtubeClient = new YoutubeClient();
        var channel = await GetChannel(youtubeClient, channelUrl);

        if (channel == null)
        {
            logger.LogWarning("Не удалось найти канал: {ChannelUrl}", channelUrl);
            yield break;
        }

        var uploads = youtubeClient.Channels.GetUploadsAsync(channel.Id);

        await foreach (var video in uploads)
        {
            yield return new()
            {
                Id = video.Id.Value,
                Title = video.Title,
                DataPath = video.Url,
                PreviewPath = video.Thumbnails.FirstOrDefault()!.Url,
            };
        }
    }

    public async Task<Channel?> GetChannel(YoutubeClient client, string channelUrl)
    {
        foreach (var parser in _parsers)
        {
            var channel = await parser(client, channelUrl);

            if (channel != null)
            {
                return channel;
            }
        }

        return null;
    }

    public MediaDto GetMediaById()
    {
        throw new NotImplementedException();
    }

    public async Task<MediaDto> Download(string videoId, Dictionary<string, string> settings)
    {
        logger.LogInformation("Начало загрузки видео: {VideoId}", videoId);

        // todo дублирование
        var youtubeClient = new YoutubeClient();

        var video = await youtubeClient.Videos.GetAsync(videoId);
        var tempPath = "E:\\bobgroup\\projects\\mediaOrcestrator\\tempDir";
        // var tempPath = "S:\\bobgroup\\projects\\mediaOrcestrator\\tempDir";
        var guid = Guid.NewGuid().ToString();
        var finalPath = Path.Combine(tempPath, guid, "media.mp4");

        Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);

        if (1 == 1)
        {
            logger.LogInformation("Используется yt-dlp для загрузки: {VideoId}", videoId);

            var ytDlp = new YtDlp("c:\\Services\\utils\\yt-dlp.exe");

            object progressLock = new();
            double oldPercent = -1;
            var currentPart = 0;

            // TODO: Подумать
            Progress<YtDlpProgress> progress = new(p =>
            {
                lock (progressLock)
                {
                    if (p.PartNumber != currentPart)
                    {
                        currentPart = p.PartNumber;
                        oldPercent = -1;
                        logger.LogInformation("Начало загрузки части #{PartNumber}", currentPart);
                    }

                    if (Math.Abs(p.Progress - oldPercent) < double.Epsilon)
                    {
                        return;
                    }

                    var isSignificantChange = Math.Abs(p.Progress - oldPercent) >= 0.1;
                    var isCompletion = p.Progress >= 1.0;
                    var isStart = oldPercent < 0;

                    if (!isSignificantChange && !isCompletion && !isStart)
                    {
                        return;
                    }

                    logger.LogInformation("Скачивание (yt-dlp) [Часть {PartNumber}]: {Percent:P0}", p.PartNumber, p.Progress);
                    oldPercent = p.Progress;
                }
            });

            logger.LogInformation("Запуск (yt-dlp) для видео: {VideoId}", videoId);

            await ytDlp.DownloadAsync($"https://www.youtube.com/watch?v={videoId}", finalPath, progress);

            logger.LogInformation("Загрузка завершена успешно (yt-dlp): {VideoId}", videoId);
        }
        else
        {
            var streamManifest = await youtubeClient.Videos.Streams.GetManifestAsync(videoId);

            var highestAudioStream = (IAudioStreamInfo)streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            var highestVideoStream = (IVideoStreamInfo)streamManifest.GetVideoOnlyStreams().GetWithHighestBitrate();

            //    var stream = DownloadItemStream.Create(0,
            //        Path.Combine(path, ".temp"),
            //        path,
            //        video,
            //        highestAudioStream,
            //        highestVideoStream);

            //    var item = DownloadItem.Create(url, [stream], video).GetValueOrDefault();

            //    Console.WriteLine("ютубный я загрузил брат ");
            //    throw new NotImplementedException();
            //}

            //private async Task DownloadCombinedStream(DownloadItemStream downloadStream, DownloadItem downloadItem, CancellationToken cancellationToken = default)
            //{

            //var tempPath = "E:\\bobgroup\\projects\\mediaOrcestrator\\tempDir";
            //var guid = Guid.NewGuid().ToString();
            var audioPath = Path.Combine(tempPath, guid, "audio.temp");
            var videoPath = Path.Combine(tempPath, guid, "video.temp");

            //if (downloadStream.AudioStreamInfo == null || downloadStream.VideoStreamInfo == null)
            //{
            ////    logger.LogError("Не удалось объединить ({MethodName}). Нет видео или аудио", nameof(DownloadCombinedStream));
            //    return;
            //}

            var token = new CancellationTokenSource();

            logger.LogDebug("Скачивание потоков для видео {VideoId}. Аудио: {AudioPath}, Видео: {VideoPath}", videoId, audioPath, videoPath);

            var audioTask = DownloadWithProgressAsync(youtubeClient,
                highestAudioStream,
                audioPath,
                token.Token);

            var videoTask = DownloadWithProgressAsync(youtubeClient,
                highestVideoStream,
                videoPath,
                token.Token);

            await Task.WhenAll(audioTask.AsTask(), videoTask.AsTask());

            logger.LogDebug("Потоки скачаны. Попытка объединить видео и аудио: {VideoId}", videoId);

            double oldPercent = -1;

            Progress<double> progress = new(percent =>
            {
                if (percent - oldPercent < 0.1)
                {
                    return;
                }

                logger.LogDebug("Объединение: {Percent:P2}", percent);
                oldPercent = percent;
            });

            // todo :) (\/)._.(\/)
            var converter = new FFmpegConverter(new("c:\\Services\\utils\\ffmpeg\\ffmpeg.exe"));
            await converter.MergeMediaAsync(finalPath, [audioPath, videoPath], progress, token.Token);

            logger.LogInformation("Успешно объединено видео и аудио: {VideoId}", videoId);
        }

        return new()
        {
            Id = videoId,
            Title = video.Title,
            Description = video.Description,
            TempDataPath = finalPath,
        };
        // bob217 -> 9I_JIereHga -> bob217
        //  logger.LogDebug("Успешно объединено видео и аудио: {Id} {StreamId}", downloadItem.Id, downloadStream.Id);
    }

    public ValueTask DownloadWithProgressAsync(YoutubeClient youtubeClient, IStreamInfo streamInfo, string path, CancellationToken cancellationToken)
    {
        double oldPercent = -1;

        var streamType = streamInfo switch
        {
            AudioOnlyStreamInfo => "Audio",
            VideoOnlyStreamInfo => "Video",
            MuxedStreamInfo => "Muxed",
            _ => "Unknown",
        };

        Progress<double> progress = new(percent =>
        {
            if (percent - oldPercent < 10)
            {
                return;
            }

            logger.LogDebug("Скачивание {StreamType}: {Percent:P2}", streamType, percent);
            oldPercent = percent;
        });

        return youtubeClient.Videos.Streams.DownloadAsync(streamInfo, path, progress, cancellationToken);
    }

    public Task<string> Upload(MediaDto media, Dictionary<string, string> settings)
    {
        logger.LogInformation("ютубный я загрузил брат: {Title}", media.Title);
        return Task.FromResult("ssshssussy!");
    }
}

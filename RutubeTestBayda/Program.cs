//using RutubeTestBayda;
//using System.Net;
//using System.Net.Http.Headers;

//internal class Program
//{
//    private static async Task Main(string[] args)
//    {
//        Console.WriteLine("Hello, World!");

//        // POST https://studio.rutube.ru/api/uploader/upload_session/?client=vulp&batch_id=bc3df789107348068ab5df2d150dd2b9
//        response {sid: "e15ab5fc93cf4feabec22de89b3d399b", video: "f4ca2eee02f1732a70eff3f933af6f80"}

//        // Request URL
//        // POST https://u.rutube.ru/upload/e15ab5fc93cf4feabec22de89b3d399b;


//        var filePath = "C:\\Users\\Max\\Videos\\test_data\\1.mkv.mp4";

//    }
//}


using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

class Program
{
    static async Task Main()
    {
        string uploadUrl = "https://u.rutube.ru/upload/e15ab5fc93cf4feabec22de89b3d399b";
     var filePath = "C:\\Users\\Max\\Videos\\test_data\\1.mkv.mp4";

        try
        {
            Console.WriteLine("Начинаю загрузку файла...");
            await UploadFileFullAsync(uploadUrl, filePath);
            Console.WriteLine("Файл успешно загружен!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Внутренняя ошибка: {ex.InnerException.Message}");
            }
        }
    }

    static async Task UploadFileFullAsync(string url, string filePath)
    {
        FileInfo fileInfo = new FileInfo(filePath);
        Console.WriteLine($"Размер файла: {fileInfo.Length} байт");

        // Читаем весь файл в память
        byte[] fileData = await File.ReadAllBytesAsync(filePath);
        Console.WriteLine($"Файл прочитан в память: {fileData.Length} байт");

        using (HttpClient client = new HttpClient())
        using (ByteArrayContent content = new ByteArrayContent(fileData))
        {
            // Устанавливаем таймауты для больших файлов
            client.Timeout = TimeSpan.FromMinutes(30);

            // Устанавливаем заголовки
            content.Headers.ContentType = new MediaTypeHeaderValue("application/offset+octet-stream");
            content.Headers.ContentLength = fileData.Length;

            // Добавляем заголовки
            client.DefaultRequestHeaders.Add("Accept", "*/*");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br, zstd");
            client.DefaultRequestHeaders.Add("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7");
            client.DefaultRequestHeaders.Add("Connection", "keep-alive");
            client.DefaultRequestHeaders.Add("Host", "u.rutube.ru");
            client.DefaultRequestHeaders.Add("Origin", "https://studio.rutube.ru");
            client.DefaultRequestHeaders.Add("Referer", "https://studio.rutube.ru/");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "empty");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "cors");
            client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-site");
            client.DefaultRequestHeaders.Add("Tus-Resumable", "1.0.0");
            client.DefaultRequestHeaders.Add("Upload-Offset", "0");
            client.DefaultRequestHeaders.Add("sec-ch-ua", "\"Google Chrome\";v=\"143\", \"Chromium\";v=\"143\", \"Not A(Brand\";v=\"24\"");
            client.DefaultRequestHeaders.Add("sec-ch-ua-mobile", "?0");
            client.DefaultRequestHeaders.Add("sec-ch-ua-platform", "\"Windows\"");

            // User-Agent через TryAddWithoutValidation
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/143.0.0.0 Safari/537.36");

            // Отправляем PATCH запрос
            Console.WriteLine("Отправляю запрос на сервер...");
            using (HttpRequestMessage request = new HttpRequestMessage(new HttpMethod("PATCH"), url))
            {
                request.Content = content;

                // Можно добавить прогресс загрузки
                using (var progress = new ProgressMessageHandler())
                {
                    progress.HttpSendProgress += (sender, args) =>
                    {
                        if (fileData.Length > 0)
                        {
                            double percentage = (double)args.BytesSent / fileData.Length * 100;
                            Console.WriteLine($"Загружено: {args.BytesSent}/{fileData.Length} байт ({percentage:F2}%)");
                        }
                    };

                    HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);

                    Console.WriteLine($"Статус ответа: {response.StatusCode}");

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Ответ сервера: {responseBody}");

                        // Проверяем Upload-Offset в заголовках ответа
                        if (response.Headers.TryGetValues("Upload-Offset", out var offsetValues))
                        {
                            Console.WriteLine($"Upload-Offset после загрузки: {string.Join(", ", offsetValues)}");
                        }
                    }
                    else
                    {
                        string errorContent = await response.Content.ReadAsStringAsync();
                        throw new HttpRequestException($"Ошибка HTTP {(int)response.StatusCode}: {response.StatusCode}\n{errorContent}");
                    }
                }
            }
        }
    }
}

// Класс для отслеживания прогресса (упрощенный вариант)
public class ProgressMessageHandler : DelegatingHandler
{
    public event EventHandler<HttpProgressEventArgs> HttpSendProgress;

    public ProgressMessageHandler() : base(new HttpClientHandler()) { }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var progressContent = new ProgressStreamContent(request.Content, this);
        request.Content = progressContent;
        return await base.SendAsync(request, cancellationToken);
    }

    private class ProgressStreamContent : HttpContent
    {
        private readonly HttpContent _originalContent;
        private readonly ProgressMessageHandler _handler;

        public ProgressStreamContent(HttpContent originalContent, ProgressMessageHandler handler)
        {
            _originalContent = originalContent;
            _handler = handler;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            var progressStream = new ProgressStream(stream, _handler);
            await _originalContent.CopyToAsync(progressStream);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = _originalContent.Headers.ContentLength ?? -1;
            return length != -1;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _originalContent.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    private class ProgressStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly ProgressMessageHandler _handler;
        private long _bytesSent;

        public ProgressStream(Stream innerStream, ProgressMessageHandler handler)
        {
            _innerStream = innerStream;
            _handler = handler;
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
            _bytesSent += count;
            _handler.HttpSendProgress?.Invoke(this, new HttpProgressEventArgs(_bytesSent, null));
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _innerStream.Write(buffer, offset, count);
            _bytesSent += count;
            _handler.HttpSendProgress?.Invoke(this, new HttpProgressEventArgs(_bytesSent, null));
        }

        // Остальные методы Stream делегируем _innerStream
        public override bool CanRead => _innerStream.CanRead;
        public override bool CanSeek => _innerStream.CanSeek;
        public override bool CanWrite => _innerStream.CanWrite;
        public override long Length => _innerStream.Length;
        public override long Position
        {
            get => _innerStream.Position;
            set => _innerStream.Position = value;
        }
        public override void Flush() => _innerStream.Flush();
        public override int Read(byte[] buffer, int offset, int count) => _innerStream.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => _innerStream.Seek(offset, origin);
        public override void SetLength(long value) => _innerStream.SetLength(value);
    }
}

public class HttpProgressEventArgs : EventArgs
{
    public long BytesSent { get; }
    public long? TotalBytes { get; }

    public HttpProgressEventArgs(long bytesSent, long? totalBytes)
    {
        BytesSent = bytesSent;
        TotalBytes = totalBytes;
    }
}
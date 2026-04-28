namespace MediaOrcestrator.Modules;

public sealed class ProgressStream(Stream baseStream, IProgress<long>? progress) : Stream
{
    private readonly Stream _baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
    private long _bytesProcessed;

    public override bool CanRead => _baseStream.CanRead;
    public override bool CanSeek => _baseStream.CanSeek;
    public override bool CanWrite => _baseStream.CanWrite;
    public override long Length => _baseStream.Length;

    public override long Position
    {
        get => _baseStream.Position;
        set => _baseStream.Position = value;
    }

    public override void Flush()
    {
        _baseStream.Flush();
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        return _baseStream.FlushAsync(cancellationToken);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return _baseStream.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        _baseStream.SetLength(value);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var n = _baseStream.Read(buffer, offset, count);
        Report(n);
        return n;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var n = await _baseStream.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
        Report(n);
        return n;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var n = await _baseStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
        Report(n);
        return n;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _baseStream.Write(buffer, offset, count);
        Report(count);
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await _baseStream.WriteAsync(buffer.AsMemory(offset, count), cancellationToken).ConfigureAwait(false);
        Report(count);
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        await _baseStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        Report(buffer.Length);
    }

    public override async ValueTask DisposeAsync()
    {
        await _baseStream.DisposeAsync().ConfigureAwait(false);
        await base.DisposeAsync().ConfigureAwait(false);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _baseStream.Dispose();
        }

        base.Dispose(disposing);
    }

    private void Report(int byteCount)
    {
        if (byteCount <= 0 || progress == null)
        {
            return;
        }

        _bytesProcessed += byteCount;
        progress.Report(_bytesProcessed);
    }
}

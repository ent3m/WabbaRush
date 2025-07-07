using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WabbajackDownloader.Core;

/// <summary>
/// A wrapper around another stream. Use this to report progress each time the inner stream is read.
/// </summary>
/// <param name="innerStream">The stream whose read progress is reported</param>
/// <param name="progress">IProgress.Report is called on this instance</param>
internal class ProgressStream(Stream innerStream, IProgress<long>? progress) : Stream
{
    private readonly Stream innerStream = innerStream;
    private readonly IProgress<long>? progress = progress;
    private long totalBytes;

    // Override ReadAsync and Read to report progress after each read. CopyAsync uses ReadAsync underneath, so it will work there too.
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        int bytesRead = await innerStream.ReadAsync(buffer, cancellationToken);
        ReportProgress(bytesRead);
        return bytesRead;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        int bytesRead = await innerStream.ReadAsync(buffer, offset, count, cancellationToken);
        ReportProgress(bytesRead);
        return bytesRead;
    }

    public override int Read(Span<byte> buffer)
    {
        int bytesRead = innerStream.Read(buffer);
        ReportProgress(bytesRead);
        return bytesRead;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int bytesRead = innerStream.Read(buffer, offset, count);
        ReportProgress(bytesRead);
        return bytesRead;
    }

    private void ReportProgress(int bytesRead)
    {
        if (bytesRead > 0)
        {
            totalBytes += bytesRead;
            progress?.Report(totalBytes);
        }
    }

    // The remaining members delegate to the underlying stream.
    public override bool CanRead => innerStream.CanRead;
    public override bool CanSeek => innerStream.CanSeek;
    public override bool CanWrite => innerStream.CanWrite;
    public override long Length => innerStream.Length;

    public override long Position
    {
        get => innerStream.Position;
        set => innerStream.Position = value;
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
        => innerStream.FlushAsync(cancellationToken);

    public override void Flush()
        => innerStream.Flush();

    public override long Seek(long offset, SeekOrigin origin)
        => innerStream.Seek(offset, origin);

    public override void SetLength(long value)
        => innerStream.SetLength(value);

    public override void Write(ReadOnlySpan<byte> buffer)
        => innerStream.Write(buffer);

    public override void Write(byte[] buffer, int offset, int count)
        => innerStream.Write(buffer, offset, count);

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        => innerStream.WriteAsync(buffer, cancellationToken);

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        => innerStream.WriteAsync(buffer, offset, count, cancellationToken);
}
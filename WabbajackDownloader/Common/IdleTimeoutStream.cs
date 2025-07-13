using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WabbajackDownloader.Common;

/// <summary>
/// A wrapper around another stream.
/// It detects idle stall, where the server stops sending bytes but never closes the connection, and throws a TimeoutException when that happens.
/// </summary>
internal class IdleTimeoutStream(Stream innerStream, TimeSpan idleTimeout) : Stream
{
    private readonly Stream innerStream = innerStream;
    private readonly TimeSpan idleTimeout = idleTimeout;

    // Override ReadAsync to report progress after each read.
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        using var idleCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        idleCts.CancelAfter(idleTimeout);

        try
        {
            return await innerStream.ReadAsync(buffer, idleCts.Token);
        }
        catch (OperationCanceledException) when (cancellationToken!.IsCancellationRequested)
        {
            throw new TimeoutException($"No data received for {idleTimeout.TotalSeconds} seconds.");
        }
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        using var idleCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        idleCts.CancelAfter(idleTimeout);

        try
        {
            return await innerStream.ReadAsync(buffer, offset, count, idleCts.Token);
        }
        catch (OperationCanceledException) when (cancellationToken!.IsCancellationRequested)
        {
            throw new TimeoutException($"No data received for {idleTimeout.TotalSeconds} seconds.");
        }
    }

    // Optionally, override the synchronous versions as well to make timeout detection works everywhere.
    public override int Read(Span<byte> buffer) => innerStream.Read(buffer);
    public override int Read(byte[] buffer, int offset, int count) => innerStream.Read(buffer, offset, count);

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

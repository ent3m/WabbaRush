using System.IO;

namespace WabbajackDownloader.Common.Hashing;

public static class ByteArrayExtensions
{
    public static async ValueTask<Hash> Hash(this byte[] data)
    {
        using var ms = new MemoryStream(data);
        return await ms.HashingCopy(Stream.Null, 512 * 1024, CancellationToken.None);
    }
}
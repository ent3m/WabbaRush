using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace WabbajackDownloader.Hashing;

/// Taken from https://github.com/wabbajack-tools/wabbajack/tree/main/Wabbajack.Hashing.xxHash64
public static class ByteArrayExtensions
{
    public static async ValueTask<Hash> Hash(this byte[] data)
    {
        using var ms = new MemoryStream(data);
        return await ms.HashingCopy(Stream.Null, CancellationToken.None);
    }
}
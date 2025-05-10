using SocketCommand.Abstractions.Interfaces;
using SocketCommand.Compression._7Zip;

namespace SocketCommand.Hosting.Defaults;

public class DefaultSocketMessageCompressor : ISocketMessageCompressor
{
    public byte[] Compress(byte[] data)
    {
        return SevenZipCompressor.CompressLZMA(data);
    }

    public byte[] Decompress(byte[] data)
    {
        return SevenZipCompressor.DecompressLZMA(data);
    }
}

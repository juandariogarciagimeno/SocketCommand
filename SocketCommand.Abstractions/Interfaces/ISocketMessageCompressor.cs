namespace SocketCommand.Abstractions.Interfaces;

public interface ISocketMessageCompressor
{
    public byte[] Compress(byte[] data);
    public byte[] Decompress(byte[] data);
}

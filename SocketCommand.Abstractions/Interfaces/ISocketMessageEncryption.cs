namespace SocketCommand.Abstractions.Interfaces
{
    public interface ISocketMessageEncryption
    {
        Task<byte[]> Encrypt(byte[] data);
        Task<byte[]> Decrypt(byte[] data);
    }
}

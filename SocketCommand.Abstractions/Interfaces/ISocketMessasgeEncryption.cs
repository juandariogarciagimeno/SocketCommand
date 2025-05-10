namespace SocketCommand.Abstractions.Interfaces
{
    public interface ISocketMessasgeEncryption
    {
        Task<byte[]> Encrypt(byte[] data);
        Task<byte[]> Decrypt(byte[] data);
    }
}

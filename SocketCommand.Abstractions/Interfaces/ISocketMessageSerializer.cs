namespace SocketCommand.Abstractions.Interfaces
{
    public interface ISocketMessageSerializer
    {
        public byte[] Serialize(object data);
        public object? Deserialize(byte[] data, Type type);
    }
}

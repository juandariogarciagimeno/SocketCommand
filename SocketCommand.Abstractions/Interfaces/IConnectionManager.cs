namespace SocketCommand.Abstractions.Interfaces
{
    public interface IConnectionManager
    {
        Task<ISocketManager?> ConnectTo(string address, int port);
        Task CloseConnection(ISocketManager socket);
        Task<List<DiscoveryResult>> Discover(int? port = null, TimeSpan? timeout = null);
    }
}

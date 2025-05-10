using SocketCommand.Abstractions.Interfaces;
using System.Net.Sockets;

namespace SocketCommand.Hosting.Core
{
    public class ConnectionManager : IConnectionManager
    {
        private SocketHost socketHost;

        internal ConnectionManager(SocketHost socketHost)
        {
            this.socketHost = socketHost;
        }

        public void CloseConnection(ISocketManager socket)
        {
        }

        public async Task<ISocketManager>? ConnectTo(string address, int port)
        {
            TcpClient socket = new TcpClient();
            await socket.ConnectAsync(address, port);
            if (socket.Connected)
            {
                return socketHost.BeginSocket(socket);
            }

            return null;
        }
    }
}

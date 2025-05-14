using Microsoft.Extensions.Options;
using SocketCommand.Abstractions.Interfaces;
using SocketCommand.Core.Config;
using SocketCommand.Hosting.Models;
using System.Net.Sockets;

namespace SocketCommand.Hosting.Core
{
    public class ConnectionManager : IConnectionManager
    {
        private readonly Dictionary<Guid, Connection> connections = [];
        private readonly IServiceProvider serviceProvider;
        private readonly SocketConfiguration config;

        public ConnectionManager(IOptions<SocketConfiguration> config, IServiceProvider serviceProvider)
        {
            this.config = config.Value;
            this.serviceProvider = serviceProvider;
        }

        public async Task CloseConnection(ISocketManager socket)
        {
            if (connections.TryGetValue(socket.Id, out var conn))
            {
                await conn.Socket.SendInternal("disconnect");
                conn.CancelToken.Cancel();
                connections.Remove(socket.Id);
            }
        }

        internal ISocketManager BeginSocket(TcpClient client)
        {
            Guid id = Guid.NewGuid();
            CancellationTokenSource tokenSource = new();
            SocketManager s = new SocketManager(client, serviceProvider, id, config.BufferSize);
            connections.Add(id, new Connection(s, tokenSource));
            tokenSource.Token.Register(s.Dispose);

            Task.Run(async () =>
            {
                await s.Start(tokenSource.Token);
            }, tokenSource.Token);

            return s;
        }

        public async Task<ISocketManager?> ConnectTo(string address, int port)
        {
            TcpClient socket = new TcpClient();
            await socket.ConnectAsync(address, port);
            if (socket.Connected)
            {
                return BeginSocket(socket);
            }

            return null;
        }
    }
}

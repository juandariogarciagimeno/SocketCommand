using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SocketCommand.Abstractions;
using SocketCommand.Abstractions.Interfaces;
using SocketCommand.Core.Config;
using SocketCommand.Hosting.Defaults;
using SocketCommand.Hosting.Models;
using System.Net;
using System.Net.Sockets;

namespace SocketCommand.Hosting.Core
{
    public class ConnectionManager : IConnectionManager
    {
        private readonly Dictionary<Guid, Connection> connections = [];
        private readonly IServiceProvider serviceProvider;
        private readonly SocketConfiguration config;
        private DefaultMessageProcessor processor;

        public ConnectionManager(IOptions<SocketConfiguration> config, IServiceProvider sp)
        {
            this.config = config.Value;
            this.serviceProvider = sp;
            this.processor = sp.GetRequiredService<DefaultMessageProcessor>();
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

        public async Task<List<DiscoveryResult>> Discover(int? port = null, TimeSpan? timeout = null)
        {
            port ??= config.UdpPort;
            timeout = timeout ?? TimeSpan.FromSeconds(5);

            UdpClient udpClient = new UdpClient();
            udpClient.EnableBroadcast = true;

            List<DiscoveryResult> found = [];

            try
            {
                var request = new DiscoveryRequest
                {
                    SecretKey = config.UdpSecret
                };

                byte[] data = await processor.Serialize(request);

                IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, config.UdpPort);
                await udpClient.SendAsync(data, data.Length, endPoint);

                DateTime start = DateTime.UtcNow;
                while((DateTime.UtcNow - start) < timeout)
                {
                    if (udpClient.Available > 0)
                    {
                        var result = await udpClient.ReceiveAsync();

                        var res = await processor.Deserialize<DiscoveryResponse>(result.Buffer);

                        if (res is DiscoveryResponse response)
                        {
                            found.Add(new DiscoveryResult
                            {
                                Address = result.RemoteEndPoint.Address.ToString(),
                                Port = response.TcpPort
                            });
                        }
                    }

                }
            }
            catch
            {
                return [];
            }

            return found;
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
    }
}

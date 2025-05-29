// --------------------------------------------------------------------------------------------------
// <copyright file="ConnectionManager.cs" company="juandariogg">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------

namespace SocketCommand.Hosting.Core;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocketCommand.Abstractions.Interfaces;
using SocketCommand.Abstractions.Models;
using SocketCommand.Hosting.Config;
using SocketCommand.Hosting.Defaults;
using SocketCommand.Hosting.Models;
using System.Net;
using System.Net.Sockets;

/// <summary>
/// Connection Manager for TCP Connections and UDP discovery.
/// </summary>
internal class ConnectionManager : IConnectionManager
{
    private readonly Dictionary<Guid, Connection> connections = [];
    private readonly IServiceProvider serviceProvider;
    private readonly SocketConfiguration config;
    private readonly DefaultMessageProcessor processor;
    private readonly ILogger<IConnectionManager> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionManager"/> class.
    /// </summary>
    /// <param name="config">Socket Configuration.</param>
    /// <param name="sp">ServiceProvider.</param>
    /// <param name="logger">Logger.</param>
    public ConnectionManager(IOptions<SocketConfiguration> config, IServiceProvider sp, ILogger<IConnectionManager> logger)
    {
        this.config = config.Value;
        this.serviceProvider = sp;
        this.processor = sp.GetRequiredService<DefaultMessageProcessor>();
        this.logger = logger;
    }

    /// <summary>
    /// Gets a list of active connections.
    /// </summary>
    public IReadOnlyCollection<ISocketManager> Connections => this.connections.Values.Select(c => c.Socket).ToList();

    /// <summary>
    /// Gets a connection by id.
    /// </summary>
    /// <param name="id">ID of the connection.</param>
    /// <returns>The ISocketManager.</returns>
    public ISocketManager GetById(Guid id)
    {
        if (this.connections.TryGetValue(id, out var conn))
        {
            return conn.Socket;
        }

        throw new KeyNotFoundException($"Connection with id {id} not found.");
    }

    /// <summary>
    /// Closes the specified connection and communicates to the connected peer the closing.
    /// </summary>
    /// <param name="socket">Connection to close.</param>
    /// <returns>A <see cref="Task"/>.</returns>
    public async Task CloseConnection(ISocketManager socket)
    {
        if (this.connections.TryGetValue(socket.Id, out var conn))
        {
            logger.LogInformation("Removing connection with id {id}", socket.Id);

            await conn.Socket.SendInternal("disconnect");
            conn.CancelToken.Cancel();
            this.connections.Remove(socket.Id);
        }
    }

    /// <summary>
    /// Connects to a remote socket.
    /// </summary>
    /// <param name="address">Address to connect to.</param>
    /// <param name="port">Port to  connect to.</param>
    /// <returns>A <see cref="ISocketManager"/> with the stablished connection.</returns>
    public async Task<ISocketManager?> ConnectTo(string address, int port)
    {
        logger.LogInformation("Trying to connect to {ip}:{port}", address, port);

        TcpClient socket = new TcpClient();
        await socket.ConnectAsync(address, port);
        if (socket.Connected)
        {
            logger.LogInformation("Connected to {ip}:{port}. Initializating socket loop.", address, port);
            return BeginSocket(socket);
        }

        return null;
    }

    /// <summary>
    /// Discovers UDP Servers sharing the configured secret key.
    /// </summary>
    /// <param name="port">Port to scan.</param>
    /// <param name="timeout">Timeout to complete the scan.</param>
    /// <returns>A List of <see cref="DiscoveryResult"/>.</returns>
    public async Task<List<DiscoveryResult>> Discover(int? port = null, TimeSpan? timeout = null)
    {
        port ??= config.UdpPort;
        timeout ??= TimeSpan.FromSeconds(5);

        UdpClient udpClient = new UdpClient()
        {
            EnableBroadcast = true,
        };

        List<DiscoveryResult> found = [];

        try
        {
            logger.LogInformation("Starting UDP discovery on port {port}", port);

            var request = new DiscoveryRequest
            {
                SecretKey = config.UdpSecret,
            };

            byte[] data = await processor.Serialize(request);

            IPEndPoint endPoint = new IPEndPoint(IPAddress.Broadcast, port.Value);
            await udpClient.SendAsync(data, data.Length, endPoint);

            DateTime start = DateTime.UtcNow;
            while (DateTime.UtcNow - start < timeout)
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
                            Port = response.TcpPort,
                        });

                        logger.LogInformation("Found server at {ip}:{port}", result.RemoteEndPoint.Address, response.TcpPort);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during UDP discovery: {message}", ex.Message);
            return [];
        }

        return found;
    }

    /// <summary>
    /// Begins a new socket connection.
    /// </summary>
    /// <param name="client">TcpClient.</param>
    /// <returns>a <see cref="ISocketManager"/>.</returns>
    internal ISocketManager BeginSocket(TcpClient client)
    {
        Guid id = Guid.NewGuid();
        CancellationTokenSource tokenSource = new CancellationTokenSource();
        SocketManager s = new SocketManager(client, serviceProvider, id, config);
        connections.Add(id, new Connection(s, tokenSource));
        tokenSource.Token.Register(s.Dispose);

        logger.LogInformation("New connection with id {id}", id);

        Task.Run(
            async () =>
        {
            await s.Start(tokenSource.Token);
        }, tokenSource.Token);

        return s;
    }
}

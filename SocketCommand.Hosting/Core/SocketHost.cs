using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SocketCommand.Abstractions.Interfaces;
using SocketCommand.Core.Config;
using System.Net.Sockets;
using System.Threading;

namespace SocketCommand.Hosting.Core;

public class SocketHost : BackgroundService
{
    private readonly TcpListener listener;
    private readonly SocketConfiguration config;
    private readonly Dictionary<Guid, CancellationTokenSource> connections = [];
    private readonly ConnectionManager connectionManager;


    public SocketHost(IOptions<SocketConfiguration> config, IConnectionManager connectionManager)
    {
        this.config = config.Value;
        this.listener = new TcpListener(System.Net.IPAddress.Any, this.config.Port);
        this.connectionManager = (ConnectionManager)connectionManager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            listener.Start();
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var client = await listener.AcceptTcpClientAsync(stoppingToken);
                    connectionManager.BeginSocket(client);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error accepting client: {ex.Message}");
                }
            }
        }
        finally
        {
            listener.Stop();
        }
    }
}

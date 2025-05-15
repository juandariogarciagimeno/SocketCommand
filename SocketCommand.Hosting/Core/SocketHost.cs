using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SocketCommand.Abstractions.Interfaces;
using SocketCommand.Core.Config;
using System.Net.Sockets;

namespace SocketCommand.Hosting.Core;

public class SocketHost : BackgroundService
{
    private readonly TcpListener listener;
    private readonly ConnectionManager connectionManager;


    public SocketHost(IOptions<SocketConfiguration> config, IConnectionManager connectionManager)
    {
        this.listener = new TcpListener(System.Net.IPAddress.Any, config.Value.Port);
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

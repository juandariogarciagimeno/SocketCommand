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
    private readonly List<CancellationTokenSource> connections = [];
    private readonly IServiceProvider serviceProvider;

    public SocketHost(IOptions<SocketConfiguration> config, IServiceProvider serviceProvider)
    {
        this.config = config.Value;
        this.listener = new TcpListener(System.Net.IPAddress.Any, this.config.Port);
        this.serviceProvider = serviceProvider;
    }

    //public async Task StartAsync(CancellationToken cancellationToken)
    //{
    //    try
    //    {
    //        listener.Start();
    //        while (!cancellationToken.IsCancellationRequested)
    //        {
    //            try
    //            {
    //                var client = await listener.AcceptTcpClientAsync(cancellationToken);
    //                BeginSocket(client);
    //            }
    //            catch (Exception ex)
    //            {
    //                Console.WriteLine($"Error accepting client: {ex.Message}");
    //            }
    //        }
    //    }
    //    finally
    //    {
    //        listener.Stop();
    //    }
    //}

    internal ISocketManager BeginSocket(TcpClient client)
    {
        CancellationTokenSource tokenSource = new ();
        tokenSource.Token.Register(client.Close);
        connections.Add(tokenSource);
        SocketManager s = new SocketManager(client, serviceProvider, config.BufferSize);

        Task.Run(async () =>
        {
            await s.Start(tokenSource.Token);
        }, tokenSource.Token);

        return s;
    }

    //public async Task StopAsync(CancellationToken cancellationToken)
    //{
    //    foreach (var token in connections)
    //    {
    //        await token.CancelAsync();
    //    }

    //    listener?.Stop();
    //}

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
                    BeginSocket(client);
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

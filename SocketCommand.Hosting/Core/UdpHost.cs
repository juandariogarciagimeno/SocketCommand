// --------------------------------------------------------------------------------------------------
// <copyright file="UdpHost.cs" company="juandariogg">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------

namespace SocketCommand.Hosting.Core;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocketCommand.Hosting.Config;
using SocketCommand.Hosting.Defaults;
using SocketCommand.Hosting.Models;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

/// <summary>
/// UDP Host is a background service that listens for incoming UDP packets.
/// </summary>
internal class UdpHost : BackgroundService
{
    private readonly UdpClient udpServer;
    private readonly SocketConfiguration config;
    private readonly DefaultMessageProcessor processor;
    private readonly ILogger<UdpHost> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UdpHost"/> class.
    /// </summary>
    /// <param name="config">Socket Configuration.</param>
    /// <param name="sp">Service Provider.</param>
    /// <param name="logger">Logger.</param>
    public UdpHost(IOptions<SocketConfiguration> config, IServiceProvider sp, ILogger<UdpHost> logger)
    {
        this.config = config.Value;
        this.logger = logger;
        this.udpServer = new UdpClient(config.Value.UdpPort, AddressFamily.InterNetwork);
        this.processor = sp.GetRequiredService<DefaultMessageProcessor>();
    }

    /// <summary>
    /// Disposes the UDP server and any other resources.
    /// </summary>
    public override void Dispose()
    {
        udpServer?.Dispose();
        base.Dispose();
    }

    /// <summary>
    /// Executes the main loop that will listen to incoming UDP packets.
    /// </summary>
    /// <param name="stoppingToken">Cancellation Token.</param>
    /// <returns>A <see cref="Task"/>.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            UdpReceiveResult remoteEP;
            logger.LogInformation("Starting UDP Listener on port: {port}", ((IPEndPoint?)udpServer.Client?.LocalEndPoint)?.Port ?? 0);
            while (!stoppingToken.IsCancellationRequested)
            {
                remoteEP = await udpServer.ReceiveAsync(stoppingToken);
                _ = Task.Run(() => TryRespond(remoteEP), stoppingToken);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("UDPServerProc exception: " + ex);
        }
        finally
        {
            udpServer.Close();
            udpServer.Dispose();
        }
    }

    /// <summary>
    /// Tries to respond to the UDP request.
    /// </summary>
    /// <param name="remoteEP">UDP request peer.</param>
    private async void TryRespond(UdpReceiveResult remoteEP)
    {
        try
        {
            logger.LogInformation("Received UDP message from {remoteEndPoint}", remoteEP.RemoteEndPoint.Address.ToString());

            var message = await processor.Deserialize<DiscoveryRequest>(remoteEP.Buffer);

            if (message is DiscoveryRequest request)
            {
                logger.LogInformation("Message is a DiscoveryRequest with secret key: {secretKey}", request.SecretKey);

                if (request.SecretKey == config.UdpSecret)
                {
                    logger.LogInformation("Secret key is valid. Sending DiscoveryResponse.");

                    var response = new DiscoveryResponse
                    {
                        TcpPort = config.Port,
                    };

                    var responseBuffer = await processor.Serialize(response);

                    await udpServer.SendAsync(responseBuffer, remoteEP.RemoteEndPoint);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing UDP message from {remoteEndPoint}", remoteEP.RemoteEndPoint.Address.ToString());
        }
    }
}

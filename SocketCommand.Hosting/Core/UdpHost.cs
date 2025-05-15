using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SevenZip.Buffer;
using SocketCommand.Abstractions.Interfaces;
using SocketCommand.Core.Config;
using SocketCommand.Hosting.Defaults;
using SocketCommand.Hosting.Models;
using System;
using System.Net.Sockets;

namespace SocketCommand.Hosting.Core
{
    internal class UdpHost : BackgroundService
    {
        private UdpClient udpServer;
        private readonly SocketConfiguration config;
        private DefaultMessageProcessor processor;

        public UdpHost(IOptions<SocketConfiguration> config, IServiceProvider sp)
        {
            this.config = config.Value;
            udpServer = new UdpClient(config.Value.UdpPort, AddressFamily.InterNetwork);
            this.processor = sp.GetRequiredService<DefaultMessageProcessor>();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                UdpReceiveResult remoteEP;

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
        }

        private async void TryRespond(UdpReceiveResult remoteEP)
        {
            byte[] buffer;
            try
            {
                var message = await processor.Deserialize<DiscoveryRequest>(remoteEP.Buffer);

                if (message is DiscoveryRequest request)
                {
                    if (request.SecretKey == config.UdpSecret)
                    {
                        var response = new DiscoveryResponse
                        {
                            TcpPort = config.Port
                        };

                        var responseBuffer = await processor.Serialize(response);

                        await udpServer.SendAsync(responseBuffer, remoteEP.RemoteEndPoint);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error processing UDP message: " + ex);
            }
        }
    }
}

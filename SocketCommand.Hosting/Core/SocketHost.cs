// --------------------------------------------------------------------------------------------------
// <copyright file="SocketHost.cs" company="juandariogg">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------

namespace SocketCommand.Hosting.Core;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SocketCommand.Abstractions.Interfaces;
using SocketCommand.Hosting.Config;
using System.Net;
using System.Net.Sockets;

/// <summary>
/// SocketHost is a background service that listens for incoming TCP connections.
/// </summary>
public class SocketHost : BackgroundService
{
    private readonly TcpListener listener;
    private readonly ConnectionManager connectionManager;
    private readonly ILogger<SocketHost> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SocketHost"/> class.
    /// </summary>
    /// <param name="config">Socket Configuration.</param>
    /// <param name="connectionManager">Connection Manager.</param>
    /// <param name="logger">Logger.</param>
    public SocketHost(IOptions<SocketConfiguration> config, IConnectionManager connectionManager, ILogger<SocketHost> logger)
    {
        this.listener = new TcpListener(System.Net.IPAddress.Any, config.Value.Port);
        this.connectionManager = (ConnectionManager)connectionManager;
        this.logger = logger;
    }

    /// <summary>
    /// Disposes the listener and any other resources.
    /// </summary>
    public override void Dispose()
    {
        listener?.Dispose();
        base.Dispose();
    }

    /// <summary>
    /// Executes the main loop that will listen to incomming connections from the socket.
    /// </summary>
    /// <param name="stoppingToken">Cancellation Token.</param>
    /// <returns>A <see cref="Task"/>.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            listener.Start();
            logger.LogInformation("Starting TCP Listener on port: {port}", ((IPEndPoint)listener.LocalEndpoint).Port);
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var client = await listener.AcceptTcpClientAsync(stoppingToken);
                    connectionManager.BeginSocket(client);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error accepting TCP connection: {message}", ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting TCP listener: {message}", ex.Message);
        }
        finally
        {
            listener.Stop();
            listener.Dispose();
        }
    }
}

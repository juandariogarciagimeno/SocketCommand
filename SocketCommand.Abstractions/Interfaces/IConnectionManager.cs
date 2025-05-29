// --------------------------------------------------------------------------------------------------
// <copyright file="IConnectionManager.cs" company="juandariogg">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------

namespace SocketCommand.Abstractions.Interfaces;

using SocketCommand.Abstractions.Models;

/// <summary>
/// Connection Manager for TCP Connections and UDP discovery.
/// </summary>
public interface IConnectionManager
{
    /// <summary>
    /// Gets a list of active connections.
    /// </summary>
    IReadOnlyCollection<ISocketManager> Connections { get; }

    /// <summary>
    /// Gets a connection by id.
    /// </summary>
    /// <param name="id">ID of the connection.</param>
    /// <returns>The ISocketManager.</returns>
    ISocketManager GetById(Guid id);

    /// <summary>
    /// Connects to a remote socket.
    /// </summary>
    /// <param name="address">Address to connect to.</param>
    /// <param name="port">Port to  connect to.</param>
    /// <returns>A <see cref="ISocketManager"/> with the stablished connection.</returns>
    Task<ISocketManager?> ConnectTo(string address, int port);

    /// <summary>
    /// Closes the specified connection and communicates to the connected peer the closing.
    /// </summary>
    /// <param name="socket">Connection to close.</param>
    /// <returns>A <see cref="Task"/>.</returns>
    Task CloseConnection(ISocketManager socket);

    /// <summary>
    /// Discovers UDP Servers sharing the configured secret key.
    /// </summary>
    /// <param name="port">Port to scan.</param>
    /// <param name="timeout">Timeout to complete the scan.</param>
    /// <returns>A List of <see cref="DiscoveryResult"/>.</returns>
    Task<List<DiscoveryResult>> Discover(int? port = null, TimeSpan? timeout = null);
}

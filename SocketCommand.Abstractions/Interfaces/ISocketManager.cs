// --------------------------------------------------------------------------------------------------
// <copyright file="ISocketManager.cs" company="juandariogg">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------

namespace SocketCommand.Abstractions.Interfaces;

/// <summary>
/// SocketManager is a class that manages the socket connection and message processing.
/// </summary>
public interface ISocketManager
{
    /// <summary>
    /// Gets the connection Id.
    /// </summary>
    public Guid Id { get; }

    /// <summary>
    /// Asynchronously send a message to the connected peer.
    /// </summary>
    /// <typeparam name="T">Type of the content to send.</typeparam>
    /// <param name="command">Command name to invoke.</param>
    /// <param name="data">Param data to be sent to the socket.</param>
    /// <returns>A <see cref="Task"/>.</returns>
    public Task Send<T>(string command, T data);

    /// <summary>
    /// synchronously sends a message to the connected peer and waits for the peer to respond. the default timeout is 5 seconds, configurable through app config.
    /// </summary>
    /// <typeparam name="TReq">Request type.</typeparam>
    /// <typeparam name="TRes">Expected response type.</typeparam>
    /// <param name="command">Command name to invoke.</param>
    /// <param name="data">Request data.</param>
    /// <returns>The response from the command.</returns>
    public Task<TRes?> Send<TReq, TRes>(string command, TReq data);

    /// <summary>
    /// Asynchronously sends a bodyless message to the connected peer.
    /// </summary>
    /// <param name="command">Command name to invoke.</param>
    /// <returns>A <see cref="Task"/>.</returns>
    public Task Send(string command);

    /// <summary>
    /// Synchronously sends a bodyless message to the connected peer and waits for the peer to respond. the default timeout is 5 seconds, configurable through app config.
    /// </summary>
    /// <typeparam name="TRes">Expected response type.</typeparam>
    /// <param name="command">Command name to invoke.</param>
    /// <returns>The response from the command.</returns>
    public Task<TRes?> Send<TRes>(string command);
}

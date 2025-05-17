// --------------------------------------------------------------------------------------------------
// <copyright file="Connection.cs" company="juandariogg">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------

namespace SocketCommand.Hosting.Models;

using SocketCommand.Hosting.Core;

/// <summary>
/// Connection class that holds the socket and cancellation token for a connection.
/// </summary>
internal class Connection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Connection"/> class.
    /// </summary>
    /// <param name="socket">Socket Connection.</param>
    /// <param name="cancelToken">Cancellation Token.</param>
    public Connection(SocketManager socket, CancellationTokenSource cancelToken)
    {
        Socket = socket;
        CancelToken = cancelToken;
    }

    /// <summary>
    /// Gets or sets Socket connection to the client.
    /// </summary>
    internal SocketManager Socket { get; set; }

    /// <summary>
    /// Gets or sets Cancellation token for the connection.
    /// </summary>
    internal CancellationTokenSource CancelToken { get; set; }
}

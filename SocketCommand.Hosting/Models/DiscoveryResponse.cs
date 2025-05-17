// --------------------------------------------------------------------------------------------------
// <copyright file="DiscoveryResponse.cs" company="juandariogg">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------

namespace SocketCommand.Hosting.Models;

using SocketCommand.Abstractions.Attributes;

/// <summary>
/// Discovery Response when the server successfully processes the discovery request and exchanged the secret key.
/// </summary>
[SocketMessage]
internal class DiscoveryResponse
{
    /// <summary>
    /// Gets or sets the Port the TCP Server is listening.
    /// </summary>
    [Order(0)]
    public int TcpPort { get; set; }
}

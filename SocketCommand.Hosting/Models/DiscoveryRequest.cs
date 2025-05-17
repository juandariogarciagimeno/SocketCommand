// --------------------------------------------------------------------------------------------------
// <copyright file="DiscoveryRequest.cs" company="juandariogg">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------

namespace SocketCommand.Hosting.Models;

using SocketCommand.Abstractions.Attributes;

/// <summary>
/// Discovery Request is a message sent to the server to discover its presence.
/// </summary>
[SocketMessage]
internal class DiscoveryRequest
{
    /// <summary>
    /// Gets or sets the SecretKey to exchange with the server.
    /// </summary>
    [Order(0)]
    public string SecretKey { get; set; } = string.Empty;
}

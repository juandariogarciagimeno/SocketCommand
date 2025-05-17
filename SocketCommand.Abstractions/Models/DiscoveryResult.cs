// --------------------------------------------------------------------------------------------------
// <copyright file="DiscoveryResult.cs" company="juandariogg">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------

namespace SocketCommand.Abstractions.Models;

/// <summary>
/// Discovery result for UDP discovery.
/// </summary>
public class DiscoveryResult
{
    /// <summary>
    /// Gets or sets Address of the UDP Server.
    /// </summary>
    public string Address { get; set; } = null!;

    /// <summary>
    /// Gets or sets the TCP port the server is listening to.
    /// </summary>
    public int Port { get; set; }
}

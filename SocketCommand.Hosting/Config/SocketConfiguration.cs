// --------------------------------------------------------------------------------------------------
// <copyright file="SocketConfiguration.cs" company="juandariogg">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------

namespace SocketCommand.Hosting.Config;

/// <summary>
/// Config model for Socket data.
/// </summary>
public class SocketConfiguration
{
    /// <summary>
    /// Gets or sets the Port for TCP connection.
    /// </summary>
    public int Port { get; set; } = 0;

    /// <summary>
    /// Gets or sets the Port for UDP connection.
    /// </summary>
    public int UdpPort { get; set; } = 5052;

    /// <summary>
    /// Gets or sets the timeout (in miliseconds) for synchronous TCP messages. Default value is 5 secondds.
    /// </summary>
    public int Timeout { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the period (in miliseconds) to send keep alive messages. Default value is 30 seconds.
    /// </summary>
    public int KeepAlivePeriod { get; set; } = 30000;

    /// <summary>
    /// Gets or sets the threshold (in number of messages) to send a keep alive message. Default value is 10 messages.
    /// </summary>
    public int KeepAliveThreshold { get; set; } = 10;

    /// <summary>
    /// Gets or sets the secret key for UDP connection.
    /// </summary>
    public string UdpSecret { get; set; } = "s3cr3t!";

    /// <summary>
    /// Gets or sets the BufferSize for TCP Messages.
    /// </summary>
    public int BufferSize { get; set; } = 1024;

    /// <summary>
    /// Gets or sets the AESKey to use for encryption.
    /// </summary>
    public string? AESKey { get; set; }

    /// <summary>
    /// Gets or sets the AESIV to use for encryption.
    /// </summary>
    public string? AESIV { get; set; }
}

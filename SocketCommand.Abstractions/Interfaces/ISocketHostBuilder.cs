// --------------------------------------------------------------------------------------------------
// <copyright file="ISocketHostBuilder.cs" company="juandariogg">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------

namespace SocketCommand.Abstractions.Interfaces;

/// <summary>
/// Bilder and configurator for socket host.
/// </summary>
public interface ISocketHostBuilder
{
    /// <summary>
    /// Adds custom compression provider to the socket communication system.
    /// </summary>
    /// <typeparam name="TCompressor">Compressor class type.</typeparam>
    /// <returns>The <see cref="ISocketHostBuilder"/>.</returns>
    ISocketHostBuilder WithCompression<TCompressor>()
        where TCompressor : class, ISocketMessageCompressor;

    /// <summary>
    /// Adds a custom serializer to the socket communication system.
    /// </summary>
    /// <typeparam name="TSerializer">Serializer class type.</typeparam>
    /// <returns>The <see cref="ISocketHostBuilder"/>.</returns>
    ISocketHostBuilder WithSerialization<TSerializer>()
        where TSerializer : class, ISocketMessageSerializer;

    /// <summary>
    /// Maps a command to a handler that will process incomming messages from the socket when matched.
    /// </summary>
    /// <param name="command">Command name identifier.</param>
    /// <param name="handler">delegate Handler for the command.</param>
    /// <returns>The <see cref="ISocketHostBuilder"/>.</returns>
    ISocketHostBuilder WithCommand(string command, Delegate handler);

    /// <summary>
    /// Adds a custom encryption provider to the socket communication system.
    /// </summary>
    /// <typeparam name="TEncryptor">Encryption class type.</typeparam>
    /// <returns>The <see cref="ISocketHostBuilder"/>.</returns>
    ISocketHostBuilder WithEncryption<TEncryptor>()
        where TEncryptor : class, ISocketMessageEncryption;

    /// <summary>
    /// Adds the default encryption provider (AES) to the socket communication system.
    /// </summary>
    /// <returns>The <see cref="ISocketHostBuilder"/>.</returns>
    ISocketHostBuilder WithAESEncryption();

    /// <summary>
    /// Adds UDP discovery to the server so it's discovereable by the clients. Needs to configure UdpPort and UdpSecret both at the server and the clients.
    /// </summary>
    /// <returns>The <see cref="ISocketHostBuilder"/>.</returns>
    ISocketHostBuilder WithUdpDiscovery();
}

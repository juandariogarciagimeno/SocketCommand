// --------------------------------------------------------------------------------------------------
// <copyright file="SocketHostBuilder.cs" company="juandariogg">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------

namespace SocketCommand.Hosting.Core;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SocketCommand.Abstractions.Interfaces;
using SocketCommand.Hosting.Config;
using SocketCommand.Hosting.Defaults;
using SocketCommand.Hosting.Models;

/// <summary>
/// Bilder and configurator for socket host.
/// </summary>
public sealed class SocketHostBuilder : ISocketHostBuilder
{
    private IServiceCollection services;

    /// <summary>
    /// Initializes a new instance of the <see cref="SocketHostBuilder"/> class.
    /// </summary>
    /// <param name="serviceCollection">Container Service Collection.</param>
    /// <param name="config">App Configuration.</param>
    internal SocketHostBuilder(IServiceCollection serviceCollection, IConfiguration config)
    {
        this.services = serviceCollection;

        serviceCollection.AddSingleton(sp => sp);
        serviceCollection.AddSingleton<IConnectionManager, ConnectionManager>();
        serviceCollection.AddSingleton<ISocketMessageSerializer, DefaultSocketMessageSerializer>();
        serviceCollection.AddSingleton<SocketHost>();
        serviceCollection.AddSingleton<DefaultMessageProcessor>();

        serviceCollection.AddHostedService<SocketHost>();
        serviceCollection.Configure<HostOptions>(options =>
        {
            options.ServicesStartConcurrently = true;
            options.ServicesStopConcurrently = true;
        });

        serviceCollection.Configure<SocketConfiguration>(config.GetSection("SocketCommand"));
    }

    /// <summary>
    /// Adds custom compression provider to the socket communication system.
    /// </summary>
    /// <typeparam name="TCompressor">Compressor class type.</typeparam>
    /// <returns>The <see cref="SocketHostBuilder"/>.</returns>
    public ISocketHostBuilder WithCompression<TCompressor>()
        where TCompressor : class, ISocketMessageCompressor
    {
        services.AddSingleton<ISocketMessageCompressor, TCompressor>();
        return this;
    }

    /// <summary>
    /// Adds a custom serializer to the socket communication system.
    /// </summary>
    /// <typeparam name="TSerializer">Serializer class type.</typeparam>
    /// <returns>The <see cref="SocketHostBuilder"/>.</returns>
    public ISocketHostBuilder WithSerialization<TSerializer>()
        where TSerializer : class, ISocketMessageSerializer
    {
        var defaultSerializer = services.FirstOrDefault(x => x.ServiceType == typeof(ISocketMessageSerializer));
        if (defaultSerializer != null)
        {
            services.Remove(defaultSerializer);
        }

        services.AddSingleton<ISocketMessageSerializer, TSerializer>();
        return this;
    }

    /// <summary>
    /// Maps a command to a handler that will process incomming messages from the socket when matched.
    /// </summary>
    /// <param name="command">Command name identifier.</param>
    /// <param name="handler">delegate Handler for the command.</param>
    /// <returns>The <see cref="SocketHostBuilder"/>.</returns>
    public ISocketHostBuilder WithCommand(string command, Delegate handler)
    {
        services.AddSingleton(new Command()
        {
            Name = command,
            Handler = handler,
        });

        return this;
    }

    /// <summary>
    /// Adds a custom encryption provider to the socket communication system.
    /// </summary>
    /// <typeparam name="TEncryptor">Encryption class type.</typeparam>
    /// <returns>The <see cref="SocketHostBuilder"/>.</returns>
    public ISocketHostBuilder WithEncryption<TEncryptor>()
        where TEncryptor : class, ISocketMessageEncryption
    {
        services.AddSingleton<ISocketMessageEncryption, TEncryptor>();
        return this;
    }

    /// <summary>
    /// Adds the default encryption provider (AES) to the socket communication system.
    /// </summary>
    /// <returns>The <see cref="SocketHostBuilder"/>.</returns>
    public ISocketHostBuilder WithAESEncryption()
    {
        return WithEncryption<DefaultSocketMessageAESEncryption>();
    }

    /// <summary>
    /// Adds UDP discovery to the server so it's discovereable by the clients. Needs to configure UdpPort and UdpSecret both at the server and the clients.
    /// </summary>
    /// <returns>The <see cref="SocketHostBuilder"/>.</returns>
    public ISocketHostBuilder WithUdpDiscovery()
    {
        services.AddSingleton<UdpHost>();
        services.AddHostedService<UdpHost>();
        return this;
    }
}

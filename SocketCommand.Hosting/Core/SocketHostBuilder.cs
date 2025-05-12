using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SocketCommand.Hosting.Commands;
using SocketCommand.Abstractions.Interfaces;
using SocketCommand.Core.Config;
using SocketCommand.Hosting.Defaults;

namespace SocketCommand.Hosting.Core;

public sealed class SocketHostBuilder
{
    private IServiceCollection serviceCollection;
    private IConfiguration config;

    internal SocketHostBuilder(IServiceCollection serviceCollection, IConfiguration config)
    {
        this.serviceCollection = serviceCollection;
        this.config = config;

        serviceCollection.AddSingleton<IServiceProvider>(sp => sp);
        serviceCollection.AddSingleton<IConnectionManager, ConnectionManager>();
        serviceCollection.AddSingleton<ISocketMessageSerializer, DefaultSocketMessageSerializer>();
        serviceCollection.AddSingleton<SocketHost>();

        serviceCollection.AddHostedService<SocketHost>();
        serviceCollection.Configure<HostOptions>(options =>
        {
            options.ServicesStartConcurrently = true;
            options.ServicesStopConcurrently = true;
        });

        serviceCollection.Configure<SocketConfiguration>(config.GetSection("SocketCommand"));
        
    }

    public SocketHostBuilder WithCompression<TCompressor>() where TCompressor : class, ISocketMessageCompressor
    {
        serviceCollection.AddSingleton<ISocketMessageCompressor, TCompressor>();
        return this;
    }

    public SocketHostBuilder WithCompression()
    {
        return WithCompression<DefaultSocketMessageCompressor>();
    }

    public SocketHostBuilder WithSerialization<TSerializer>() where TSerializer : class, ISocketMessageSerializer
    {
        var defaultSerializer = serviceCollection.FirstOrDefault(x => x.ServiceType == typeof(ISocketMessageSerializer));
        if (defaultSerializer != null)
        {
            serviceCollection.Remove(defaultSerializer);
        }

        serviceCollection.AddSingleton<ISocketMessageSerializer, TSerializer>();
        return this;
    }

    public SocketHostBuilder WithCommand(string command, Delegate handler)
    {
        serviceCollection.AddSingleton(new Command()
        {
            Name = command,
            Handler = handler
        });

        return this;
    }

    public SocketHostBuilder WithEncryption<TEncryptor>() where TEncryptor : class, ISocketMessasgeEncryption
    {
        serviceCollection.AddSingleton<ISocketMessasgeEncryption, TEncryptor>();
        return this;
    }

    public SocketHostBuilder WithAESEncryption()
    {
        return WithEncryption<DefaultSocketMessageAESEncryption>();
    }

}

using Microsoft.Extensions.Hosting;
using SocketCommand.Hosting.Core;

namespace SocketCommand.Hosting;

public static class DependencyContainer
{
    public static SocketHostBuilder AddSocketCommand(this IHostApplicationBuilder builder)
    {
        var sb = new SocketHostBuilder(builder.Services, builder.Configuration);
        return sb;
    }

    public static IHostBuilder AddSocketCommand(this IHostBuilder builder, Action<SocketHostBuilder> configure)
    {
        builder.ConfigureServices((context, services) =>
        {
            var sb = new SocketHostBuilder(services, context.Configuration);
            configure(sb);
        });

        return builder;
    }
}

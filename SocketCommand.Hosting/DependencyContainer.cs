using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SocketCommand.Abstractions.Interfaces;
using SocketCommand.Core.Config;
using SocketCommand.Hosting.Core;
using SocketCommand.Hosting.Defaults;

namespace SocketCommand.Hosting;

public static class DependencyContainer
{
    public static SocketHostBuilder AddSocketCommand(this IHostApplicationBuilder builder)
    {

        var sb = new SocketHostBuilder(builder.Services, builder.Configuration);
        return sb;
    }
}

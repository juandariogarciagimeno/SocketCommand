// --------------------------------------------------------------------------------------------------
// <copyright file="DependencyContainer.cs" company="juandariogg">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------

namespace SocketCommand.Hosting;

using Microsoft.Extensions.Hosting;
using SocketCommand.Abstractions.Interfaces;
using SocketCommand.Hosting.Core;

/// <summary>
/// Dependency Container for Socket Command.
/// </summary>
public static class DependencyContainer
{
    /// <summary>
    /// Adds Socket Command to the Host Builder.
    /// </summary>
    /// <param name="builder">Host Application Builder.</param>
    /// <returns>A Socket Host Builder.</returns>
    public static ISocketHostBuilder AddSocketCommand(this IHostApplicationBuilder builder)
    {
        var sb = new SocketHostBuilder(builder.Services, builder.Configuration);
        return sb;
    }

    /// <summary>
    /// Adds Socket Command to the Host Builder.
    /// </summary>
    /// <param name="builder">Host Builder.</param>
    /// <param name="configure">Delegate to configure the SocketHostBuilder.</param>
    /// <returns>The Host Builder.</returns>
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

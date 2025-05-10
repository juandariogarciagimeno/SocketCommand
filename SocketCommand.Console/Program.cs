using Microsoft.Extensions.Hosting;
using SocketCommand.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.AddSocketCommand();

var host = builder.Build();

await host.StartAsync();
await host.WaitForShutdownAsync();
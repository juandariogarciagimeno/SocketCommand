using Microsoft.Extensions.Hosting;
using SocketCommand.Abstractions.Attributes;
using SocketCommand.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder
    .AddSocketCommand()
    .WithCompression()
    .WithAESEncryption()
    .WithCommand("test", (TestObject o) =>
    {
        Console.WriteLine(o.Name);
    });

var host = builder.Build();

await host.StartAsync();
await host.WaitForShutdownAsync();


[SocketMessage]
public class TestObject
{
    [Order(1)]
    public int Id { get; set; }

    [Order(2)]
    public string Name { get; set; }
}
using Microsoft.Extensions.Hosting;
using SocketCommand.Abstractions.Attributes;
using SocketCommand.Abstractions.Interfaces;
using SocketCommand.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder
    .AddSocketCommand()
    .WithCompression()
    .WithAESEncryption()
    .WithCommand("test", async (TestObject o, ISocketManager caller) =>
    {
        Console.WriteLine(o.Name);
        o.Id = 5;
        o.Name = "Paco";
        await caller.Send("test", o);
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
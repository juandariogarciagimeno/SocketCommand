using Microsoft.Extensions.Hosting;
using SocketCommand.Abstractions.Attributes;
using SocketCommand.Abstractions.Interfaces;
using SocketCommand.Hosting;

var builder = Host.CreateDefaultBuilder(args);
builder.AddSocketCommand((sb) =>
{
    sb
    .WithAESEncryption()
    .WithCompression()
    .WithUdpDiscovery()
    .WithCommand("ping", async (ISocketManager caller) =>
    {
        Console.WriteLine("ping");
        await caller.Send("ping");
    })
    .WithCommand("testdata", async (TestObject o) =>
    {
        Console.WriteLine($"Id = {o.Id}, Name = {o.Name}");
    });
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
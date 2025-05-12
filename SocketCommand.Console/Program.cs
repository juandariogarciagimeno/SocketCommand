using Microsoft.Extensions.Hosting;
using SocketCommand.Abstractions.Attributes;
using SocketCommand.Abstractions.Interfaces;
using SocketCommand.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder
    .AddSocketCommand()
    .WithCompression()
    .WithAESEncryption()
    .WithCommand("test", async (ISocketManager caller) =>
    {
        Console.WriteLine("test");
        var testObj = new TestObject() { Id = 3, Name = "Manolo" };
        await caller.Send("test", testObj);
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
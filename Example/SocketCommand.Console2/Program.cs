using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SocketCommand.Abstractions.Attributes;
using SocketCommand.Abstractions.Interfaces;
using SocketCommand.Hosting;
using SocketCommand.Hosting.Core;

var builder = Host.CreateApplicationBuilder(args);
builder.AddSocketCommand()
    .WithCompression()
.WithAESEncryption()
    .WithCommand("ping", async (ISocketManager caller) =>
    {
        Console.WriteLine("ping");
        await caller.Send("ping");
    });

var host = builder.Build();

await host.StartAsync();

await Task.Delay(5000);

var connectionManager = host.Services.GetRequiredService<IConnectionManager>();
var socketManager = await connectionManager.ConnectTo("127.0.0.1", 5001);

var testObj = new TestObject() { Id = 3, Name = "Test" };
await socketManager.Send("testdata", testObj);

await connectionManager.CloseConnection(socketManager);

await Task.Delay(5000);

socketManager = await connectionManager.ConnectTo("127.0.0.1", 5001);
await socketManager.Send("ping");

await host.WaitForShutdownAsync();

[SocketMessage]
public class TestObject
{
    [Order(1)]
    public int Id { get; set; }

    [Order(2)]
    public string Name { get; set; }
}
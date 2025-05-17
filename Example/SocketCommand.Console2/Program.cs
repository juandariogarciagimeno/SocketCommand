using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SocketCommand.Abstractions.Interfaces;
using SocketCommand.Hosting;
using SocketCommand.Abstractions.Attributes;
using SocketCommand.Compression._7Zip;

var builder = Host.CreateApplicationBuilder(args);
builder.AddSocketCommand()
    .With7ZipCompression()
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
var discoveries = await connectionManager.Discover();
var found = discoveries.FirstOrDefault();

var connection = await connectionManager.ConnectTo(found.Address, found.Port);

var testObj = new TestObject() { Id = 3, Name = "Test" };
await connection.Send("testdata", testObj);

await connectionManager.CloseConnection(connection);

await Task.Delay(5000);

connection = await connectionManager.ConnectTo(found.Address, found.Port);
await connection.Send("ping");

await host.WaitForShutdownAsync();

[SocketMessage]
public class TestObject
{
    [Order(1)]
    public int Id { get; set; }

    [Order(2)]
    public string Name { get; set; }
}
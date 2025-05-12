using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SocketCommand.Abstractions.Attributes;
using SocketCommand.Abstractions.Interfaces;
using SocketCommand.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.AddSocketCommand()
    .WithCompression()
    .WithAESEncryption();

var host = builder.Build();

await host.StartAsync();

await Task.Delay(5000);

var connectionManager = host.Services.GetRequiredService<IConnectionManager>();
var socketManager = await connectionManager.ConnectTo("127.0.0.1", 5001);

var testObj = new TestObject() { Id = 3, Name = "Manolo" };
var res = await socketManager.Send<TestObject>("test");

await host.WaitForShutdownAsync();

[SocketMessage]
public class TestObject
{
    [Order(1)]
    public int Id { get; set; }

    [Order(2)]
    public string Name { get; set; }
}
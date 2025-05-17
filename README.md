# ⚡ SocketCommand

[![NuGet](https://img.shields.io/nuget/v/SocketCommand.Hosting.svg?style=flat-square)](https://www.nuget.org/packages/SocketCommand.Hosting/)
[![NuGet](https://img.shields.io/nuget/dt/SocketCommand.Hosting.svg?style=flat-square)](https://www.nuget.org/packages/SocketCommand.Hosting/)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg?style=flat-square)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8%2B-blueviolet.svg?style=flat-square)](https://dotnet.microsoft.com/)


SocketCommand is a modern, extensible NuGet package that makes **socket-based command communication** simple and powerful in .NET applications.

Built on top of **.NET hosting**, it supports:

- 🔁 Command-based duplex communication
- 🧱 Entity serialization
- 🔒 AES encryption
- 📦 Compression
- 📡 UDP-based peer discovery
- 💉 Dependency Injection-friendly design

---

## 📦 Installation

SocketCommand consists of two packages:

| Package | Purpose |
|--------|---------|
| `SocketCommand.Abstractions` | 🎯 Lightweight project with interfaces and attributes. Add this **wherever you define socket-bound entities**. |
| `SocketCommand.Hosting` | 🚀 Core socket server/client logic. Add this to your **startup project**. |
| `SocketCommand.Compression.7Zip` | 🗜️Compress messages using 7Zip/LZMA. Add this to your **startup project**. |

Install via NuGet:

```bash
dotnet add package SocketCommand.Hosting
dotnet add package SocketCommand.Abstractions
dotnet add package SocketCommand.Compression.7Zip
```

---

# ⚙️ Configuration

Define your settings in `appsettings.json`:
```json
{
  "SocketCommand": {
    "Port": 5000,
    "BufferSize": 1024,
    "UdpPort": 5052,
    "UdpSecret": "s3cr3t!",
    "AESKey": "jiwlQEjVDC67rIbte8n0XoZZzgXE4cSnRlj81YZaAf4=",
    "AESIV": "ivgVH9nmCwAafSK2jhyA1Q=="
  }
}
```

| Property           | Description                                                         |
| ------------------ | ------------------------------------------------------------------- |
| `Port`             | TCP server port. Use `0` for a random port.                         |
| `BufferSize`       | Size of the read buffer. Default: `1024`.                           |
| `UdpPort`          | Port for UDP discovery.                                             |
| `UdpSecret`        | Shared secret to verify discovery packets. Must match on both ends. |
| `AESKey` / `AESIV` | 🔐 AES encryption key and IV. Both must match across peers.         |

---
# 💄 Decorating Entities

Entities that are to be used as commands or responses must be decorated with the `SocketCommandAttribute` and `OrderAttribute`. The order attribute must batch on entities on both ends.

```csharp
[SocketMessage]
public class TestEntity
{
    [Order(1)]
    public int Id { get; set; }

    [Order(2)]
    public string Name { get; set; }
}
```

---

# 🚀 Usage

### Host Initialization

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddSocketCommand()
    .With7ZipCompression()
    .WithAESEncryption()
    .WithCommand("ping", async (ISocketManager caller) =>
    {
        Console.WriteLine("ping");
        await caller.Send("ping");
    });

await builder.Build().StartAsync();
```

### Connecting as Client

```csharp
var connectionManager = host.Services.GetRequiredService<IConnectionManager>();
var discoveries = await connectionManager.Discover();

var found = discoveries.FirstOrDefault();
if (found != null)
{
    var connection = await connectionManager.ConnectTo(found.Address, found.Port);
    await connection.Send("testdata", new TestObject { Id = 3, Name = "Test" });
}
```

### Full Example with DI and Logging

```csharp
var builder = Host.CreateDefaultBuilder(args);

builder.AddSocketCommand(sb =>
{
    sb.WithAESEncryption()
      .With7ZipCompression()
      .WithUdpDiscovery()
      .WithCommand("ping", async (ISocketManager caller) =>
      {
          Console.WriteLine("ping");
          await caller.Send("ping");
      })
      .WithCommand("testdata", async (TestObject obj, ILogger<TestObject> logger) =>
      {
          logger.LogInformation("Received: {Id} - {Name}", obj.Id, obj.Name);
      });
});

await builder.Build().StartAsync();
```

# 🧩 Customization

You can plug in your own compression or encryption strategies:

```csharp
builder.AddSocketCommand()
    .WithCompression<CustomCompressor>()
    .WithEncryption<CustomEncryptor>();
```
Implement:

- `ISocketMessageCompressor`
- `ISocketMessageEncryption`

and register them via generics or manual DI.

# 🛠️ Requirements

- .NET 8
- Works in console apps, Windows services, and ASP.NET Core projects
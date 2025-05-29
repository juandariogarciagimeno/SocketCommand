// --------------------------------------------------------------------------------------------------
// <copyright file="SocketManager.cs" company="juandariogg">
// Licensed under the MIT license. See LICENSE file in the samples root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------

namespace SocketCommand.Hosting.Core;

using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Text;
using SocketCommand.Hosting.Defaults;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using SocketCommand.Hosting.Models;
using SocketCommand.Abstractions.Interfaces;
using SocketCommand.Hosting.Config;

/// <summary>
/// SocketManager is a class that manages the socket connection and message processing.
/// </summary>
public sealed class SocketManager : ISocketManager, IDisposable
{
    private const byte INTERNALCOMMAND = 0x01;
    private const byte USERCOMMAND = 0x02;
    private static readonly byte[] BOM = [0xEF, 0xBB, 0xBF];

    private readonly TcpClient socket;
    private readonly NetworkStream stream;
    private readonly StreamWriter writer;
    private readonly int bufferSize = 1024;
    private readonly int timeout = 5000;

    private readonly IServiceProvider serviceProvider;
    private readonly DefaultMessageProcessor processor;
    private readonly ISocketMessageSerializer serializer;
    private readonly IConnectionManager connectionManager;
    private readonly ManualResetEventSlim syncSemaphore = new ManualResetEventSlim();
    private readonly ILogger<ISocketManager> logger;

    private readonly IEnumerable<Command> handlers;
    private readonly IList<Command> synchronousHandlers = [];
    private readonly IList<Command> internalHandlers = [];

    private readonly Guid id;

    private readonly int keepAlivePeriod = 30000;
    private readonly int keepAliveThreshold = 10;
    private int keepAliveFailedCount = 0;
    private Timer _keepAliveTimer;

    /// <summary>
    /// Initializes a new instance of the <see cref="SocketManager"/> class.
    /// </summary>
    /// <param name="socket">TCP Socket Connection.</param>
    /// <param name="serviceProvider">Service Provider.</param>
    /// <param name="id">Id of the connection.</param>
    /// <param name="config">Socket Configuration.</param>
    public SocketManager(TcpClient socket, IServiceProvider serviceProvider, Guid id, SocketConfiguration config)
    {
        this.socket = socket;
        this.stream = socket.GetStream();
        this.writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
        this.id = id;
        this.bufferSize = config.BufferSize;
        this.timeout = config.Timeout;
        this.keepAliveThreshold = config.KeepAliveThreshold;
        this.keepAlivePeriod = config.KeepAlivePeriod;
        this.serviceProvider = serviceProvider;
        this.connectionManager = serviceProvider.GetRequiredService<IConnectionManager>();
        this.processor = serviceProvider.GetRequiredService<DefaultMessageProcessor>();
        this.serializer = serviceProvider.GetRequiredService<ISocketMessageSerializer>();
        this.handlers = serviceProvider.GetServices<Command>();
        this.logger = serviceProvider.GetRequiredService<ILogger<ISocketManager>>();

        this.internalHandlers =
        [
            new Command()
            {
                Name = "disconnect",
                Handler = () =>
                {
                    connectionManager.CloseConnection(this);
                },
            },
            new Command()
            {
                Name = "keepalive",
                Handler = () =>
                {
                    Interlocked.Exchange(ref keepAliveFailedCount, 0);
                },
            },
        ];
    }

    /// <summary>
    /// Gets the connection Id.
    /// </summary>
    public Guid Id => id;

    /// <summary>
    /// Disposes the instance properties and finishes the connection.
    /// </summary>
    public void Dispose()
    {
        _keepAliveTimer?.Dispose();
        syncSemaphore?.Dispose();
        writer?.Dispose();
        stream?.Dispose();
        socket?.Close();
        socket?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Asynchronously send a message to the connected peer.
    /// </summary>
    /// <typeparam name="T">Type of the content to send.</typeparam>
    /// <param name="command">Command name to invoke.</param>
    /// <param name="data">Param data to be sent to the socket.</param>
    /// <returns>A <see cref="Task"/>.</returns>
    public async Task Send<T>(string command, T data)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(data);

            byte[] header = ComputeHeader(command);

            var serializedData = serializer.Serialize(data);
            serializedData = header.Concat(serializedData).ToArray();

            serializedData = await processor.CompressAndEncrypt(serializedData);

            await stream.WriteAsync(serializedData, 0, serializedData.Length);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending data to the connected peer.");
        }
    }

    /// <summary>
    /// Asynchronously sends a bodyless message to the connected peer.
    /// </summary>
    /// <param name="command">Command name to invoke.</param>
    /// <returns>A <see cref="Task"/>.</returns>
    public async Task Send(string command)
    {
        byte[] serializedData = await processor.CompressAndEncrypt(ComputeHeader(command));

        await stream.WriteAsync(serializedData, 0, serializedData.Length);
    }

    /// <summary>
    /// Synchronously sends a bodyless message to the connected peer and waits for the peer to respond. the default timeout is 5 seconds, configurable through app config.
    /// </summary>
    /// <typeparam name="TRes">Expected response type.</typeparam>
    /// <param name="command">Command name to invoke.</param>
    /// <returns>The response from the command.</returns>
    public async Task<TRes?> Send<TRes>(string command)
    {
        syncSemaphore.Reset();
        try
        {
            TRes? result = default;
            synchronousHandlers.Add(new Command()
            {
                Name = command,
                Handler = (TRes r) =>
                {
                    result = r;
                    syncSemaphore.Set();
                },
            });

            await Send(command);
            syncSemaphore.Wait(timeout);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending data to the connected peer.");
            return default;
        }
        finally
        {
            try
            {
                synchronousHandlers.Remove(synchronousHandlers.Last());
            }
            finally
            {
                syncSemaphore.Set();
            }
        }
    }

    /// <summary>
    /// synchronously sends a message to the connected peer and waits for the peer to respond. the default timeout is 5 seconds, configurable through app config.
    /// </summary>
    /// <typeparam name="TReq">Request type.</typeparam>
    /// <typeparam name="TRes">Expected response type.</typeparam>
    /// <param name="command">Command name to invoke.</param>
    /// <param name="data">Request data.</param>
    /// <returns>The response from the command.</returns>
    public async Task<TRes?> Send<TReq, TRes>(string command, TReq data)
    {
        try
        {
            TRes? result = default;
            synchronousHandlers.Add(new Command()
            {
                Name = command,
                Handler = (TRes r) =>
                {
                    result = r;
                    syncSemaphore.Set();
                },
            });

            await Send(command, data);
            syncSemaphore.Wait(timeout);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending data to the connected peer.");
            return default;
        }
        finally
        {
            try
            {
                synchronousHandlers.Remove(synchronousHandlers.Last());
            }
            finally
            {
                syncSemaphore.Set();
            }
        }
    }

    /// <summary>
    /// Asynchronously receives a message from the connected peer.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The received data.</returns>
    public async Task<byte[]?> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            byte[] buffer = new byte[bufferSize];
            List<byte> data = new List<byte>();
            int size = 0;
            do
            {
                size = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                if (size > 0)
                {
                    data.AddRange(buffer.Take(size));
                }
            }
            while (size > 0 && size >= bufferSize);

            return [.. data];
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error receiving data from the connected peer.");
            return null;
        }
    }

    /// <summary>
    /// Sends an internal message to the connected peer.
    /// </summary>
    /// <param name="command">Command name to invoke.</param>
    /// <returns>A <see cref="Task"/>.</returns>
    internal async Task SendInternal(string command)
    {
        byte[] serializedData = await processor.CompressAndEncrypt(ComputeHeader(command, true));

        await stream.WriteAsync(serializedData, 0, serializedData.Length);
    }

    /// <summary>
    /// Starts the loop that will listen for incomming messages and invoke commands.
    /// </summary>
    /// <param name="token">Cancellation Token.</param>
    /// <returns>A <see cref="Task"/>.</returns>
    internal async Task Start(CancellationToken token = default)
    {
        BeginKeepAlive(token);
        await MainLoop(token);
    }

    private async Task MainLoop(CancellationToken token = default)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var data = await ReceiveAsync(token);
                if (data == null)
                {
                    break;
                }

                if (data.SequenceEqual(BOM))
                {
                    continue;
                }

                data = await processor.DecryptAndDecompress(data);

                if (data == null)
                {
                    continue;
                }

                HandleMessage(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    private void BeginKeepAlive(CancellationToken token = default)
    {
        _keepAliveTimer = new Timer(
            state =>
            {
                _ = Task.Run(
                    async () =>
                    {
                        if (token.IsCancellationRequested || !socket.Connected)
                        {
                            return;
                        }

                        try
                        {
                            if (Interlocked.Increment(ref keepAliveFailedCount) > keepAliveThreshold)
                            {
                                logger.LogWarning("Keepalive failed too many times, disconnecting.");
                                await connectionManager.CloseConnection(this);
                                _keepAliveTimer?.Dispose();
                                return;
                            }

                            await SendInternal("keepalive");
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error sending keepalive message.");
                        }
                    });
            },
            null,
            TimeSpan.Zero,
            TimeSpan.FromMilliseconds(keepAlivePeriod));
    }

    /// <summary>
    /// Handles the received message and if its a valid command will invoke it.
    /// </summary>
    /// <param name="data">received data.</param>
    internal void HandleMessage(byte[] data)
    {
        var (commandName, id, body, isInternal) = ParseHeader(data);

        Command? handler = null;
        if (isInternal)
        {
            handler = internalHandlers.FirstOrDefault(x => x.Name == commandName);
        }
        else
        {
            handler = synchronousHandlers.FirstOrDefault(x => x.Name == commandName);
            handler ??= handlers.FirstOrDefault(x => x.Name == commandName);
        }

        if (handler != null)
        {
            using var scope = serviceProvider.CreateScope();
            var actionType = handler.Handler.GetType();
            var commandParameterType = handler.Handler.Method.GetParameters()?.FirstOrDefault()?.ParameterType;
            var parameters = body.Length > 0 && commandParameterType != null ? serializer.Deserialize(body, commandParameterType) : default;
            List<object> arguments = new List<object>();
            foreach (var p in handler.Handler.Method.GetParameters())
            {
                try
                {
                    if (p.ParameterType.IsAssignableTo(parameters?.GetType()))
                    {
                        handler.Caster ??= CreateCaster(p.ParameterType);

                        var casted = handler.Caster(parameters);
                        arguments.Add(casted);
                        continue;
                    }
                }
                catch
                {
                }

                var service = scope.ServiceProvider.GetService(p.ParameterType);
                if (service != null)
                {
                    arguments.Add(service);
                    continue;
                }

                if (p.ParameterType.IsAssignableTo(typeof(ISocketManager)))
                {
                    arguments.Add(this);
                    continue;
                }
            }

            handler.Handler.DynamicInvoke([.. arguments]);
        }
    }

    /// <summary>
    /// Computes the message header.
    /// </summary>
    /// <param name="command">Command Name.</param>
    /// <param name="isinternal">Whether the command is internal or user-made.</param>
    /// <returns>the codified byte array.</returns>
    private byte[] ComputeHeader(string command, bool isinternal = false)
    {
        var h = Encoding.ASCII.GetBytes(command);
        var h2 = id.ToByteArray();
        byte[] header = new byte[33];
        Array.Copy(h, header, h.Length);
        Array.Copy(h2, 0, header, 16, h2.Length);
        header[32] = isinternal ? INTERNALCOMMAND : USERCOMMAND;
        return header;
    }

    /// <summary>
    /// Parses the header of the message.
    /// </summary>
    /// <param name="header">header data.</param>
    /// <returns>A tuple of data contained in the header. The command Name, the Id of the peer connection, the body of the messasge and whether the command is internal.</returns>
    private (string command, Guid id, byte[] body, bool isInternal) ParseHeader(byte[] header)
    {
        var command = Encoding.ASCII.GetString(header.Take(16).ToArray()).TrimEnd('\0');
        var id = new Guid(header.Skip(16).Take(16).ToArray());
        var isInternal = header[32] == INTERNALCOMMAND;
        var body = header.Skip(33).ToArray();
        return (command, id, body, isInternal);
    }

    /// <summary>
    /// Creates a caster for the given type.
    /// </summary>
    /// <param name="targetType">Dynamic type.</param>
    /// <returns>A func for casting types.</returns>
    private static Func<object, object> CreateCaster(Type targetType)
    {
        var param = Expression.Parameter(typeof(object), "input");
        var converted = Expression.Convert(Expression.Convert(param, targetType), typeof(object));
        var lambda = Expression.Lambda<Func<object, object>>(converted, param);
        return lambda.Compile();
    }
}

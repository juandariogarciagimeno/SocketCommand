using Microsoft.Extensions.DependencyInjection;
using SocketCommand.Hosting.Models;
using SocketCommand.Abstractions.Interfaces;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks.Dataflow;
using SocketCommand.Hosting.Defaults;

namespace SocketCommand.Hosting.Core;

public sealed class SocketManager : ISocketManager, IDisposable
{
    private static byte[] BOM = [0xEF, 0xBB, 0xBF];
    private static byte INTERNAL_COMMAND = 0x01;
    private static byte USER_COMMAND = 0x02;

    private readonly TcpClient socket;
    private readonly NetworkStream stream;
    private readonly StreamWriter writer;
    private readonly int bufferSize = 1024;
    private readonly IServiceProvider serviceProvider;
    private readonly DefaultMessageProcessor processor;
    private readonly ISocketMessageSerializer serializer;
    private readonly IConnectionManager connectionManager;
    private IEnumerable<Command> handlers;
    private IList<Command> synchronousHandlers = [];
    private ManualResetEventSlim syncSemaphore = new();

    private IList<Command> internalHandlers = [];

    private Guid id;

    public SocketManager(TcpClient socket, IServiceProvider serviceProvider, Guid id, int bufferSize = 1024)
    {
        this.socket = socket;
        this.stream = socket.GetStream();
        this.writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
        this.id = id;
        this.bufferSize = bufferSize;
        this.serviceProvider = serviceProvider;
        this.connectionManager = serviceProvider.GetRequiredService<IConnectionManager>();
        this.processor = serviceProvider.GetRequiredService<DefaultMessageProcessor>();
        this.serializer = serviceProvider.GetRequiredService<ISocketMessageSerializer>();
        this.handlers = serviceProvider.GetServices<Command>();

        internalHandlers =
        [
            new Command()
            {
                Name = "disconnect",
                Handler = () =>
                {
                    connectionManager.CloseConnection(this);
                },
            },
        ];
    }

    public Guid Id => id;

    private byte[] ComputeHeader(string command, bool isinternal = false)
    {
        var h = Encoding.ASCII.GetBytes(command);
        var h2 = id.ToByteArray();
        byte[] header = new byte[33];
        Array.Copy(h, header, h.Length);
        Array.Copy(h2, 0, header, 16, h2.Length);
        header[32] = isinternal ? INTERNAL_COMMAND : USER_COMMAND;
        return header;
    }

    private (string command, Guid id, byte[] body, bool isInternal) ParseHeader(byte[] header)
    {
        var command = Encoding.ASCII.GetString(header.Take(16).ToArray()).TrimEnd('\0');
        var id = new Guid(header.Skip(16).Take(16).ToArray());
        var isInternal = header[32] == INTERNAL_COMMAND;
        var body = header.Skip(33).ToArray();
        return (command, id, body, isInternal);
    }

    public async Task Send<T>(string command, T data)
    {
        try
        {
            byte[] header = ComputeHeader(command);

            var serializedData = serializer.Serialize(data);
            serializedData = header.Concat(serializedData).ToArray();

            serializedData = await processor.CompressAndEncrypt(serializedData);

            await stream.WriteAsync(serializedData, 0, serializedData.Length);
        }
        catch { }
    }

    public async Task Send(string command)
    {
        byte[] serializedData = await processor.CompressAndEncrypt(ComputeHeader(command));

        await stream.WriteAsync(serializedData, 0, serializedData.Length);
    }

    public async Task<TRes> Send<TRes>(string command)
    {
        
        syncSemaphore.Reset();
        try
        {
            TRes result = default;
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
            syncSemaphore.Wait(5000);

            return result;
        }
        catch
        {
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

    public async Task<TRes> Send<TReq, TRes>(string command, TReq data)
    {
        try
        {
            TRes result = default;
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
            syncSemaphore.Wait(5000);

            return result;
        }
        catch
        {
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

    internal async Task SendInternal(string command)
    {
        byte[] serializedData = await processor.CompressAndEncrypt(ComputeHeader(command, true));

        await stream.WriteAsync(serializedData, 0, serializedData.Length);
    }

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
            } while (size > 0 && size >= bufferSize);

            return [.. data];
        }
        catch { return null; }
    }

    internal async Task Start(CancellationToken token = default)
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
                    continue;

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

    public void HandleMessage(byte[] data)
    {
        var (commandName, id, body, isInternal) = ParseHeader(data);

        Command? handler = null;
        if (isInternal)
        {
            handler = internalHandlers.FirstOrDefault(x => x.Name == commandName);
        } else
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
                        if (handler.Caster == null)
                        {
                            handler.Caster = CreateCaster(p.ParameterType);
                        }

                        var casted = handler.Caster(parameters);
                        arguments.Add(casted);
                    }
                }
                catch 
                {
                    arguments.Add(scope.ServiceProvider.GetService(p.ParameterType));
                }

                if (p.ParameterType.IsAssignableTo(typeof(ISocketManager)))
                {
                    arguments.Add(this);
                }
            }

            handler.Handler.DynamicInvoke([.. arguments]);
        }
    }

    public void Dispose()
    {
        writer?.Dispose();
        stream?.Dispose();
        socket?.Close();
        socket?.Dispose();
        GC.SuppressFinalize(this);
    }

    private static Func<object, object> CreateCaster(Type targetType)
    {
        var param = Expression.Parameter(typeof(object), "input");
        var converted = Expression.Convert(Expression.Convert(param, targetType), typeof(object));
        var lambda = Expression.Lambda<Func<object, object>>(converted, param);
        return lambda.Compile();
    }
}

using Microsoft.Extensions.DependencyInjection;
using SocketCommand.Abstractions.Command;
using SocketCommand.Abstractions.Interfaces;
using System.Linq.Expressions;
using System.Net.Sockets;
using System.Text;

namespace SocketCommand.Hosting.Core;

public sealed class SocketManager : ISocketManager, IDisposable
{
    private static byte[] BOM = [0xEF, 0xBB, 0xBF];

    private readonly TcpClient socket;
    private readonly NetworkStream stream;
    private readonly StreamReader reader;
    private readonly StreamWriter writer;
    private readonly int bufferSize = 1024;
    private readonly IServiceProvider serviceProvider;
    private readonly ISocketMessageSerializer serializer;
    private readonly ISocketMessageCompressor? compressor;
    private readonly ISocketMessasgeEncryption? encryptor;
    private IEnumerable<Command> handlers;


    public SocketManager(TcpClient socket, IServiceProvider serviceProvider, int bufferSize = 1024)
    {
        this.socket = socket;
        this.stream = socket.GetStream();
        this.reader = new StreamReader(stream, Encoding.UTF8);
        this.writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
        this.bufferSize = bufferSize;
        this.serviceProvider = serviceProvider;
        this.serializer = serviceProvider.GetRequiredService<ISocketMessageSerializer>();
        this.compressor = serviceProvider.GetService<ISocketMessageCompressor>();
        this.encryptor = serviceProvider.GetService<ISocketMessasgeEncryption>();
        this.handlers = serviceProvider.GetServices<Command>();
    }

    public async Task Send<T>(string command, T data)
    {
        try
        {
            var h = Encoding.ASCII.GetBytes(command);
            byte[] header = new byte[8];
            Array.Copy(h, header, h.Length);

            var serializedData = serializer.Serialize(data);
            serializedData = header.Concat(serializedData).ToArray();
            if (compressor != null)
            {
                serializedData = compressor.Compress(serializedData);
            }

            if (encryptor != null)
            {
                serializedData = await encryptor.Encrypt(serializedData);
            }

            await stream.WriteAsync(serializedData, 0, serializedData.Length);
        }
        catch { }
    }

    public Task<TRes> Send<TReq, TRes>(string command, TReq data)
    {
        throw new NotImplementedException();
    }

    public async Task<byte[]?> ReceiveAsync(CancellationToken cancellationToken)
    {
        try
        {
            byte[] buffer = new byte[bufferSize];
            List<byte> data = new List<byte>();
            int size = 0;
            do
            {
                size = await socket.GetStream().ReadAsync(buffer, 0, buffer.Length, cancellationToken);
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

                if (encryptor != null)
                {
                    data = await encryptor.Decrypt(data);
                }

                if (compressor != null)
                {
                    data = compressor.Decompress(data);
                }

                if (data == null)
                {
                    continue;
                }

                HandleMessage(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                break;
            }
        }
    }

    public void HandleMessage(byte[] data)
    {
        var header = data.Take(8).ToArray();
        var commandName = Encoding.ASCII.GetString(header).TrimEnd('\0');
        data = data.Skip(8).ToArray();

        var handler = handlers.FirstOrDefault(x => x.Name == commandName);
        if (handler != null)
        {
            var actionType = handler.Handler.GetType();
            var invoke = actionType.GetMethod("Invoke");
            var commandParameterType = invoke.GetParameters()[0].ParameterType;
            var parameters = serializer.Deserialize(data, commandParameterType);
            List<object> arguments = new List<object>();
            foreach (var p in invoke.GetParameters())
            {
                try
                {
                    if (handler.Caster == null)
                    {
                        handler.Caster = CreateCaster(p.ParameterType);
                    }

                    var casted = handler.Caster(parameters);
                    arguments.Add(casted);
                }
                catch { arguments.Add(serviceProvider.GetService(p.ParameterType)); }
            }

            invoke.Invoke(handler.Handler, arguments.ToArray());
        }
    }
    public void Dispose()
    {
        reader?.Dispose();
        writer?.Dispose();
        stream?.Dispose();
        socket?.Close();
        socket?.Dispose();
        GC.SuppressFinalize(this);
    }

    public static Func<object, object> CreateCaster(Type targetType)
    {
        var param = Expression.Parameter(typeof(object), "input");
        var converted = Expression.Convert(Expression.Convert(param, targetType), typeof(object));
        var lambda = Expression.Lambda<Func<object, object>>(converted, param);
        return lambda.Compile();
    }

}

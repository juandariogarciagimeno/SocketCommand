using SocketCommand.Abstractions.Interfaces;
using SocketCommand.Hosting.Core;

namespace SocketCommand.Hosting.Models;

internal class Connection
{
    public Connection(SocketManager socket, CancellationTokenSource cancelToken)
    {
        Socket = socket;
        CancelToken = cancelToken;
    }

    internal SocketManager Socket { get; set; }
    internal CancellationTokenSource CancelToken { get; set; }
}

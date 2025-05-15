using SocketCommand.Abstractions.Attributes;

namespace SocketCommand.Hosting.Models
{
    [SocketMessage]
    internal class DiscoveryResponse
    {
        [Order(0)]
        public int TcpPort { get; set; }
    }
}

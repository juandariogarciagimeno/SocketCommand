using SocketCommand.Abstractions.Attributes;

namespace SocketCommand.Hosting.Models
{
    [SocketMessage]
    internal class DiscoveryRequest
    {
        [Order(0)]
        public string SecretKey { get; set; } = string.Empty;
    }
}

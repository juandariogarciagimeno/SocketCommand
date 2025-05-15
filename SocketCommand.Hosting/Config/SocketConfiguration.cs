namespace SocketCommand.Core.Config;
public class SocketConfiguration
{
    public int Port { get; set; } = 0;
    public int UdpPort { get; set; } = 5052;
    public string UdpSecret { get; set; } = "s3cr3t!";
    public int BufferSize { get; set; } = 1024;
    public string? AESKey { get; set; }
    public string? AESIV { get; set; }
}

namespace SocketCommand.Core.Config;
public class SocketConfiguration
{
    public int Port { get; set; } = 5000;
    public int BufferSize { get; set; } = 1024;
    public string? AESKey { get; set; }
    public string? AESIV { get; set; }
}

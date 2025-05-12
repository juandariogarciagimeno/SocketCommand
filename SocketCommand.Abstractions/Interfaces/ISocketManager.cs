namespace SocketCommand.Abstractions.Interfaces;

public interface ISocketManager
{
    public Task Send<T>(string command, T data);
    public Task<TRes> Send<TReq, TRes>(string command, TReq data);
    public Task Send(string command);
    public Task<TRes> Send<TRes>(string command);
}

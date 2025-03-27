using System.Net.Sockets;
using PatStrapServer.PatStrap;

namespace PatStrapServer.protocol;

public interface IProtocol
{
    public Service? ServiceInstance { get; set; }
    protected Socket? Socket { get; }

    public int MaxHaptics { get; }

    public Task ConnectAsync(string ipAddress, ushort port);

    // Do some protocol work 
    public Task DoWork();
}
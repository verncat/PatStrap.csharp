using System.Net.Sockets;
using PatStrapServer.PatStrap;

namespace PatStrapServer.protocol;

public class ModernProtocol: IProtocol
{
    public Service? ServiceInstance { get; set; }

    public Task DoWork()
    {
        throw new NotImplementedException();
    }
}
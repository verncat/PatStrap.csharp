using System.Net.Sockets;
using PatStrapServer.PatStrap;

namespace PatStrapServer.protocol;

public class ModernProtocol: IProtocol
{
    public Service? ServiceInstance { get; set; }

    public Task DoWork(Service service)
    {
        throw new NotImplementedException();
    }
}
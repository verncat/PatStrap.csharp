using System.Net.Sockets;
using PatStrapServer.PatStrap;

namespace PatStrapServer.protocol;

public interface IProtocol
{
    public Service? ServiceInstance { get; set; }
    
    // Do some protocol work 
    public Task DoWork(Service service);
}
using System.Net;
using System.Net.Sockets;
using BuildSoft.OscCore;
using PatStrapServer.PatStrap;
using VRC.OSCQuery;

namespace PatStrapServer.Osc;

public class HapticTriggerEventArgs(string name, float value)
{
    public readonly string ContactName = name;
    public readonly float Value = value;
};

public class Service
{
    public delegate void HapticTriggerEvent(object sender, HapticTriggerEventArgs e);

    public event HapticTriggerEvent? OnHapticTrigger;
    
    private OscServer _oscReceiver;
    
    public Service()
    {
    }

    private static IPAddress GetLocalIpAddress() 
    {
        return IPAddress.Loopback;
    }

    private static IPAddress? GetLocalIpAddressNonLoopback()
    {
        // Get the host name of the local machine
        string hostName = Dns.GetHostName();

        // Get the IP address of the first IPv4 network interface found on the local machine
        return Dns.GetHostEntry(hostName).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
    }

    public void Register()
    {
        var tcpPort = Extensions.GetAvailableTcpPort();
        var udpPort = Extensions.GetAvailableUdpPort();
        
        var oscQuery = new OSCQueryServiceBuilder()
            .WithServiceName($"PatStrap-{udpPort}")
            .WithHostIP(GetLocalIpAddress())
            .WithOscIP(GetLocalIpAddressNonLoopback())
            .WithTcpPort(tcpPort)
            .WithUdpPort(udpPort)
            .StartHttpServer()
            .AdvertiseOSC()
            .AdvertiseOSCQuery()
            .Build();
        oscQuery.RefreshServices();
        
        // Manually logging the ports to see them without a logger
        Console.WriteLine($"Started OSCQueryService at TCP {oscQuery.TcpPort}, UDP {oscQuery.OscPort}");
        
        _oscReceiver = OscServer.GetOrCreate(udpPort);
        
        oscQuery.AddEndpoint<float>("/avatar/parameters/pat_right", Attributes.AccessValues.WriteOnly);
        _oscReceiver.TryAddMethod("/avatar/parameters/pat_right",
            (message) =>
            {
                var value = message.ReadFloatElement(0);
                OnHapticTrigger?.Invoke(this, new HapticTriggerEventArgs("pat_right", value));
                Console.WriteLine($"pat_right {value}");
            }
        );
        
        oscQuery.AddEndpoint<float>("/avatar/parameters/pat_left", Attributes.AccessValues.WriteOnly);
        _oscReceiver.TryAddMethod("/avatar/parameters/pat_left",
            (message) =>
            {
                var value = message.ReadFloatElement(0);
                OnHapticTrigger?.Invoke(this, new HapticTriggerEventArgs("pat_left", value));
                Console.WriteLine($"pat_left {value}");
            }
        );
        
        oscQuery.OnOscQueryServiceAdded += async (OSCQueryServiceProfile  profile) =>
        {
            Console.WriteLine($"New QS: {profile.name}");
            // var tree = await Extensions.GetOSCTree(profile.address, profile.port);
            // Console.WriteLine(tree);
        };
    }
}
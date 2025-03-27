using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using BuildSoft.OscCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PatStrapServer.PatStrap;
using VRC.OSCQuery;

namespace PatStrapServer.Osc;

public class HapticTriggerEventArgs(string name, float value)
{
    public readonly string ContactName = name;
    public readonly float Value = value;
};

public class Service : BackgroundService
{
    public delegate void HapticTriggerEvent(object sender, HapticTriggerEventArgs e);

    public event HapticTriggerEvent? OnHapticTrigger;

    private ILogger<Service> _logger;
    
    private OscServer _oscReceiver;
    private OscClient? _oscSender = null;
    private Stopwatch _batterySenderTimer = new Stopwatch();
    
    public Service(ILogger<Service> logger)
    {
        _logger = logger;
        _batterySenderTimer.Start();
        Register();
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

    public void Run(PatStrap.Service service)
    {
        if (_batterySenderTimer.ElapsedMilliseconds < 10_000)
            return;
        
        _oscSender?.Send("/avatar/parameters/patstrap_battery_value", service.BatteryLevel / 100.0f);
        
        _batterySenderTimer.Restart();
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
        _logger.LogInformation($"Started OSCQueryService at TCP {oscQuery.TcpPort}, UDP {oscQuery.OscPort}");
        
        _oscReceiver = OscServer.GetOrCreate(udpPort); 
        
        _oscSender = new OscClient("127.0.0.1", 9000);

        // todo: dynamically endpoint register
        
        oscQuery.AddEndpoint<float>("/avatar/parameters/pat_right", Attributes.AccessValues.WriteOnly);
        _oscReceiver.TryAddMethod("/avatar/parameters/pat_right",
            (message) =>
            {
                var value = message.ReadFloatElement(0);
                OnHapticTrigger?.Invoke(this, new HapticTriggerEventArgs("pat_right", value));
                _logger.LogDebug($"pat_right {value}");
            }
        );

        
        oscQuery.AddEndpoint<float>("/avatar/parameters/pat_back", Attributes.AccessValues.WriteOnly);
        _oscReceiver.TryAddMethod("/avatar/parameters/pat_back",
            (message) =>
            {
                var value = message.ReadFloatElement(0);
                OnHapticTrigger?.Invoke(this, new HapticTriggerEventArgs("pat_back", value));
                _logger.LogDebug($"undefined_2 {value}");
            }
        );

        
        oscQuery.AddEndpoint<float>("/avatar/parameters/pat_back2", Attributes.AccessValues.WriteOnly);
        _oscReceiver.TryAddMethod("/avatar/parameters/pat_back2",
            (message) =>
            {
                var value = message.ReadFloatElement(0);
                OnHapticTrigger?.Invoke(this, new HapticTriggerEventArgs("pat_back2", value));
                _logger.LogDebug($"undefined_3 {value}");
            }
        );
        
        oscQuery.AddEndpoint<float>("/avatar/parameters/pat_left", Attributes.AccessValues.WriteOnly);
        _oscReceiver.TryAddMethod("/avatar/parameters/pat_left",
            (message) =>
            {
                var value = message.ReadFloatElement(0);
                OnHapticTrigger?.Invoke(this, new HapticTriggerEventArgs("pat_left", value));
                _logger.LogDebug($"pat_left {value}");
            }
        );

        oscQuery.OnOscServiceAdded += profile =>
        {
            _logger.LogInformation($"New Osc Service: {profile.name}");

        };
        
        oscQuery.OnOscQueryServiceAdded += async (OSCQueryServiceProfile  profile) =>
        {
            _logger.LogInformation($"New QS: {profile.name}");
            // var tree = await Extensions.GetOSCTree(profile.address, profile.port);
            // Console.WriteLine(tree);
        };
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var f = this;
        return Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                OnHapticTrigger?.Invoke(f, new HapticTriggerEventArgs("pat_right", 1));
                await Task.Delay(1000, stoppingToken);
            }        
        }, stoppingToken);
        // return Task.CompletedTask;
    }
}
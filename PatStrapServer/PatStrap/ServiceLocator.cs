using System.Diagnostics;
using Makaretu.Dns;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PatStrapServer.PatStrap;

public abstract class BasePatStrapServiceInfo(string ipAddress, ushort port)
{
    public string IpAddress { set; get; } = ipAddress;
    public ushort Port { set; get; } = port;
}

public class PatStrapLegacyServiceInfo(string ipAddress, ushort port) : BasePatStrapServiceInfo(ipAddress, port)
{
    public const string ServiceDefaultDomain = "_http._tcp.local";
    public const string ServiceInstanceDefaultDomain = "patstrap." + ServiceDefaultDomain;
}

public class PatStrapModernServiceInfo(string ipAddress, ushort port) : BasePatStrapServiceInfo(ipAddress, port)
{
    public const string ServiceDefaultDomain = "_patstrap._udp.local";
    public const string ServiceInstanceDefaultDomain = "patstrap." + ServiceDefaultDomain;
}

public class ServiceLocator(ILogger<ServiceLocator> logger) : BackgroundService
{
    public delegate void PatstrapServiceLocatedEvent(object sender, BasePatStrapServiceInfo e);

    public event PatstrapServiceLocatedEvent? PatstrapServiceLocated;

    private MulticastService? _multicastService;
    private ServiceDiscovery? _serviceDiscovery;

    private BasePatStrapServiceInfo? _serviceInLocate = null;

    private static bool IsServiceName(DomainName name)
    {
        return name == PatStrapLegacyServiceInfo.ServiceDefaultDomain ||
               name == PatStrapModernServiceInfo.ServiceDefaultDomain;
    }

    private static bool IsServiceInstanceName(DomainName name)
    {
        return name == PatStrapLegacyServiceInfo.ServiceInstanceDefaultDomain ||
               name == PatStrapModernServiceInfo.ServiceInstanceDefaultDomain;
    }

    public void RegisterListeners()
    {
        _multicastService = new MulticastService();
        _serviceDiscovery = new ServiceDiscovery(_multicastService);
        
        _serviceDiscovery.ServiceDiscovered += (s, serviceName) =>
        {
            if (!IsServiceName(serviceName))
                return;

            // Ask for the name of instances of the service.
            _multicastService.SendQuery(serviceName, type: DnsType.PTR);
        };
        _multicastService.AnswerReceived += (s, e) =>
        {
            var ptrs = e.Message.Answers.OfType<PTRRecord>();
            foreach (var ptr in ptrs)
            {
                if (!IsServiceName(ptr.Name) && !IsServiceInstanceName(ptr.DomainName)) 
                    continue;

                logger.LogInformation($"PatStrap service resolved. Send QU to PatStrap ({ptr.DomainName})");
                _multicastService.SendQuery(ptr.DomainName, type: DnsType.SRV);
            }

            var srvs = e.Message.Answers.OfType<SRVRecord>();
            foreach (var addr in srvs)
            {
                if (addr.Name == PatStrapLegacyServiceInfo.ServiceInstanceDefaultDomain)
                    _serviceInLocate ??= new PatStrapLegacyServiceInfo("", addr.Port);
                else if (addr.Name == PatStrapModernServiceInfo.ServiceInstanceDefaultDomain)
                    _serviceInLocate ??= new PatStrapModernServiceInfo("", addr.Port);
                else
                    continue;

                _multicastService.SendQuery(addr.Target, type: DnsType.A);
                _multicastService.SendQuery(addr.Target, type: DnsType.AAAA);
            }

            var addrs = e.Message.Answers.OfType<ARecord>();
            foreach (var addr in addrs)
            {
                if (addr.Name != "patstrap.local" || _serviceInLocate == null) continue;

                logger.LogInformation($"Got IPv4 {addr.Address}");
                _serviceInLocate.IpAddress = addr.Address.ToString();

                PatstrapServiceLocated?.Invoke(this, _serviceInLocate);
            }

            var addrs6 = e.Message.Answers.OfType<AAAARecord>();
            foreach (var addr in addrs6)
            {
                if (addr.Name != "patstrap.local" || _serviceInLocate == null) continue;

                logger.LogInformation($"Got IPv6 {addr.Address}");
                _serviceInLocate.IpAddress = addr.Address.ToString();

                PatstrapServiceLocated?.Invoke(this, _serviceInLocate);
            }
        };
        _multicastService.NetworkInterfaceDiscovered += (s, e) =>
        {
            _serviceDiscovery.QueryAllServices();
        };
        _multicastService.Start();
    }

    public void Start()
    {
        _multicastService.Start();
    }

    ~ServiceLocator()
    {
        _multicastService.Stop();
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }
}
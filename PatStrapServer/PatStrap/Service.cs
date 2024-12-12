using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PatStrapServer.protocol;

namespace PatStrapServer.PatStrap;

public enum HapticAreaType
{
    LeftEar,
    RightEar
};

public class HapticValue(float value)
{
    // Current value
    public float Value = value;

    // Previous sent value
    public float LastValue = -1;

    public bool IsNeedSend() => Math.Abs(LastValue - Value) > float.Epsilon;
}

public class Service(ILogger<Service> logger) : BackgroundService
{
    internal Socket? _socket;
    private IProtocol _proto = null!;
    public bool IsRunning { get; private set; } = false;

    internal byte _batteryLevel = 0;
    public byte BatteryLevel => _batteryLevel;

    public Dictionary<HapticAreaType, HapticValue> Haptics { get; }
        = new Dictionary<HapticAreaType, HapticValue>()
    {
        { HapticAreaType.LeftEar, new HapticValue(0) },
        { HapticAreaType.RightEar, new HapticValue(0) },
    };
    

    public async Task ConnectAsync(IProtocol proto, string ipAddress, ushort port)
    {
        _proto = proto;
        _proto.ServiceInstance = this;
        
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        await _socket.ConnectAsync(IPAddress.Parse(ipAddress), port);

        logger.LogInformation($"Connected to {ipAddress}:{port} Using {_proto.GetType().Name}");
        IsRunning = true;
    }

    public void SetHapticValue(HapticAreaType areaType, float value)
    {
        Haptics[areaType].LastValue = Haptics[areaType].Value;
        Haptics[areaType].Value = value;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();
        while (!stoppingToken.IsCancellationRequested)
        {
            if (!IsRunning)
                continue;
        
            await DoWork();
            
            logger.LogInformation($"Battery: {BatteryLevel}%");
        }        
        await Task.CompletedTask;
    }
    
    public async Task DoWork()
    {
        if (!IsRunning)
                return;
    
        await _proto.DoWork();
        
        logger.LogInformation($"Battery: {BatteryLevel}%");
    }

    ~Service()
    {
        _socket?.Close();
    }
}
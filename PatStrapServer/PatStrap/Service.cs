using System.Net;
using System.Net.Sockets;
using System.Text;
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

public class Service
{
    internal Socket? _socket;
    private IProtocol _proto;
    public bool IsRunning { get; private set; } = false;

    internal byte _batteryLevel = 0;
    public byte BatteryLevel => _batteryLevel;

    public Dictionary<HapticAreaType, HapticValue> Haptics { get; }


    public Service(IProtocol proto)
    {
        _proto = proto;
        _proto.ServiceInstance = this;
        
        Haptics = new Dictionary<HapticAreaType, HapticValue>()
        {
            { HapticAreaType.LeftEar, new HapticValue(0) },
            { HapticAreaType.RightEar, new HapticValue(0) },
        };
    }

    public async Task ConnectAsync(string ipAddress, ushort port)
    {
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        await _socket.ConnectAsync(IPAddress.Parse(ipAddress), port);

        Console.WriteLine($"Connected to {ipAddress}:{port} Using {_proto.GetType().Name}");
        IsRunning = true;
    }

    public void SetHapticValue(HapticAreaType areaType, float value)
    {
        Haptics[areaType].LastValue = Haptics[areaType].Value;
        Haptics[areaType].Value = value;
    }

    public async Task Run()
    {
        if (IsRunning)
        {
            await _proto.DoWork(this);
        }
    }

    ~Service()
    {
        _socket?.Close();
    }
}
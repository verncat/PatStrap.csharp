using System.Net;
using System.Net.Sockets;
using PatStrapServer.PatStrap;

namespace PatStrapServer.protocol;

public class ModernProtocol : IProtocol
{
    public Service? ServiceInstance { get; set; }
    public Socket? Socket { get; private set; }
    public int MaxHaptics { get; } = 6;

    private IPEndPoint? _endPoint;

    private long _lastHash = 0;

    public float VCC = 3.3f;

    public float minValue = 0.4f;
    public float maxValue = 1;
    public float zeroThreshhold = 0.1f;

    public List<string> mapping = new List<string>()
    {
        "LeftEar",
        "RightEar",
        "LeftMidHead",
        "RightMidHead"
    };

    public async Task ConnectAsync(string ipAddress, ushort port)
    {
        _endPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
        Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
    }

    public double NonLinearMapValue(double value)
    {
        if (value < zeroThreshhold)
            return 0;
        if (value < 0 || value > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Value must be in the range [0; 1].");
        }

        return minValue + (maxValue - minValue) * (1 - Math.Pow(1 - value, 2));
    }

    private async Task WritePatStateAsync(byte[] buffer)
    {
        var outBuffer = new byte[MaxHaptics];
        var bytesToCopy = Math.Min(buffer.Length, outBuffer.Length);
        Array.Copy(buffer, outBuffer, bytesToCopy);

        var bytesSent = await Socket!.SendToAsync(outBuffer, _endPoint!);
    }

    public async Task DoWork()
    {
        var newHash = ServiceInstance!.lastChangedTime;
        if (_lastHash == newHash || newHash == 0)
            return;

        _lastHash = newHash;
        
        // Todo: mapping
        var output = new byte[MaxHaptics];
        var i = 0;
        // foreach (var contactName in mapping)
        // {
        //     output[i++] = (byte)(ServiceInstance!.Haptics[contactName].GetValue() * 255);
        // }
        var haptics = ServiceInstance!.Haptics.Take(MaxHaptics);

        foreach (var pair in haptics)
        {
            output[i++] = (byte)(pair.Value.GetValue() * 255);
        }
        
        await WritePatStateAsync(output);
        
    }
}
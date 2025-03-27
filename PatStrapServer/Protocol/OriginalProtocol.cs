using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using PatStrapServer.PatStrap;

namespace PatStrapServer.protocol;

public class OriginalProtocol: IProtocol
{

    public int MaxHaptics { get; } = 2;
    
    private long _lastHash = 0;
    public Service? ServiceInstance { get; set; }
    public Socket? Socket { get; private set; }

    public async Task ConnectAsync(string ipAddress, ushort port) {
        
        Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        await Socket.ConnectAsync(IPAddress.Parse(ipAddress), port);
    }

    Service? IProtocol.ServiceInstance
    {
        get => ServiceInstance;
        set => ServiceInstance = value;
    }

    private async Task<byte> ReadBatteryLevelAsync()
    {
        var responseBytes = new byte[1];
        var bytes = await Socket!.ReceiveAsync(responseBytes);
        return responseBytes[0];
    }

    private async Task WritePatStateAsync(float left, float right)
    {
        var data = ((int)((1 - left) * 15) << 4) | (int)((1 - right) * 15);
                
        var intBytes = BitConverter.GetBytes(data);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(intBytes);
                    
        var bytesSent = await Socket!.SendAsync(intBytes);
        Console.WriteLine($"Sent {bytesSent} bytes");
    }
    

    private async Task SendPat(float left, float right)
    {
        Debug.Assert(ServiceInstance != null, nameof(ServiceInstance) + " != null");

        await WritePatStateAsync(left, right);
    }

    public async Task DoWork()
    {
        // Retrieve battery level
        var batteryLevel = await ReadBatteryLevelAsync();
        ServiceInstance!._batteryLevel = batteryLevel;

        var newHash = ServiceInstance!.lastChangedTime;
        if (_lastHash == newHash || newHash == 0)
            return;

        _lastHash = newHash;
        // Send haptics state if not sent previously
        // Todo: mapping
        // Получаем первые два элемента и записываем их в переменные
        var firstTwoElements = ServiceInstance.Haptics.Take(MaxHaptics).ToList();

        float leftEarHaptic = 0, rightEarHaptic = 0;
        if (firstTwoElements.Count >= 1)
        {
            leftEarHaptic = firstTwoElements[0].Value.GetValue();
        }
        if (firstTwoElements.Count >= 2)
        {
            leftEarHaptic = firstTwoElements[1].Value.GetValue();
        }
        
        await SendPat(leftEarHaptic, rightEarHaptic);
    }
}
using System.Diagnostics;
using System.Net.Sockets;
using PatStrapServer.PatStrap;

namespace PatStrapServer.protocol;

public class OriginalProtocol: IProtocol
{
    public Service? ServiceInstance { get; set; }

    Service? IProtocol.ServiceInstance
    {
        get => ServiceInstance;
        set => ServiceInstance = value;
    }

    public async Task<byte> ReadBatteryLevelAsync(Socket socket)
    {
        var responseBytes = new byte[1];
        var bytes = await socket.ReceiveAsync(responseBytes);
        return responseBytes[0];
    }

    public async Task WritePatStateAsync(Socket socket, float left, float right)
    {
        var data = ((int)((1 - left) * 15) << 4) | (int)((1 - right) * 15);
                
        var intBytes = BitConverter.GetBytes(data);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(intBytes);
                    
        var bytesSent = await socket.SendAsync(intBytes);
        Console.WriteLine($"Sent {bytesSent} bytes");
    }
    

    private async Task SendPat(float left, float right)
    {
        Debug.Assert(ServiceInstance != null, nameof(ServiceInstance) + " != null");
        
        ServiceInstance.Haptics[HapticAreaType.LeftEar].LastValue = left;
        ServiceInstance.Haptics[HapticAreaType.RightEar].LastValue = right;

        await WritePatStateAsync(ServiceInstance._socket!, left, right);
    }

    public async Task DoWork(Service service)
    {
        // Retrieve battery level
        var batteryLevel = await ReadBatteryLevelAsync(service._socket!);
        service._batteryLevel = batteryLevel;
        Console.WriteLine($"Battery: {(int)batteryLevel}%");

        // Send haptics state if not sent previously
        var leftEarHaptic = service.Haptics[HapticAreaType.LeftEar];
        var rightEarHaptic = service.Haptics[HapticAreaType.RightEar];
        
        if (leftEarHaptic.IsNeedSend() || rightEarHaptic.IsNeedSend())
            await SendPat(leftEarHaptic.Value, rightEarHaptic.Value);
    }
}
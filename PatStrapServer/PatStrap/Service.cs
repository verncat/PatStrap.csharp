using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PatStrapServer.Config;
using PatStrapServer.protocol;

namespace PatStrapServer.PatStrap;


public class HapticValue(float value)
{
    // Current value
    public float Value = value;

    // Previous sent value
    public float LastValue = -1;

    public float MinValue = 0;

    public float MaxValue = 1;

    public float ZeroThreshold = 0;

    private float MapValue(float value)
    {
        if (value < ZeroThreshold)
            return 0;
        if (value < 0 || value > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(value), "Value must be in the range [0; 1].");
        }

        return MinValue + (MaxValue - MinValue) * value;
    }

    public float GetValue()
    {
        return MapValue(Value);
    }

    public bool IsNeedSend() => Math.Abs(LastValue - Value) > float.Epsilon;
}

public class OnHapticUpdatedArgs(string name, HapticValue value)
{
    public string Name = name;
    public HapticValue Value = value;
}

public delegate void OnHapticUpdatedEvent(object sender, OnHapticUpdatedArgs args);
public delegate void OnHapticListUpdated(object sender);


public class Service(ILogger<Service> logger, IConfiguration configuration) : BackgroundService
{
    public IProtocol _proto = null!;
    public bool IsRunning { get; private set; } = false;

    internal byte _batteryLevel = 0;
    public byte BatteryLevel => _batteryLevel;

    public long lastChangedTime = 0;

    public AppSettings? AppSettings;
    public event OnHapticUpdatedEvent? OnHapticUpdated;
    public event OnHapticListUpdated? OnHapticListUpdated;

    public Dictionary<string, HapticValue> Haptics { get; }
        = new()
    {
        
    };

    public async Task ConnectAsync(IProtocol proto, string ipAddress, ushort port)
    {
        _proto = proto;
        _proto.ServiceInstance = this;
        
        logger.LogInformation($"Connecting to {ipAddress}:{port} Using {_proto.GetType().Name}");

        await _proto.ConnectAsync(ipAddress, port);

        logger.LogInformation($"Connected.");
        IsRunning = true;
    }

    public void SetHapticValue(string areaType, float value)
    {
        Haptics[areaType].LastValue = Haptics[areaType].Value;
        Haptics[areaType].Value = value;
        lastChangedTime = DateTime.UtcNow.Ticks;
        
        OnHapticUpdated?.Invoke(this, new OnHapticUpdatedArgs(areaType, Haptics[areaType]));
    }
    
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Считывание данных из конфигурации
        AppSettings = configuration?.GetSection("AppSettings")?.Get<AppSettings>();

        if (AppSettings?.CalliData != null)
        {
            // Использование данных
            foreach (var calli in AppSettings.CalliData)
            {
                var contactName = calli.ContactName;
                contactName ??= "";

                Haptics[contactName] = new HapticValue(0)
                {
                    MaxValue = calli.Max,
                    MinValue = calli.Min,
                    ZeroThreshold = calli.ZeroThreshold
                };
            }

            OnHapticListUpdated?.Invoke(this);
        }

        return Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (!IsRunning)
                    continue;
        
                await DoWork();
            
                // logger.LogInformation($"Battery: {BatteryLevel}%");
            }        
        }, stoppingToken);
    }

    private async Task DoWork()
    {
        if (!IsRunning)
            return;
    
        await _proto.DoWork();
        
        // logger.LogInformation($"Battery: {BatteryLevel}%");
    }

    public void AddHaptic(string s)
    {
        Haptics[s] = new HapticValue(0);
        OnHapticListUpdated?.Invoke(this);
        AppSettings.SaveConfiguration(this);
        
    }
    public void RemoveHaptic(string s)
    {
        Haptics.Remove(s);
        OnHapticListUpdated?.Invoke(this);
        AppSettings.SaveConfiguration(this);
        
    }
}
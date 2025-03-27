using PatStrapServer.PatStrap;

namespace PatStrapServer.Config;

public class CalliData
{
    public float Min { get; set; }
    public float Max { get; set; }
    public float ZeroThreshold { get; set; }
    public string? ContactName { get; set; }
}

public class AppSettings
{
    public float Power { get; set; }
    public List<CalliData> CalliData { get; set; }
    
    public static void SaveConfiguration(Service service)
    {
        var appSettings = new AppSettings()
        {
            CalliData = new()
        };
        
        foreach (var haptic in service.Haptics)
        {
            appSettings.CalliData.Add(new CalliData()
            {
                ContactName = haptic.Key,
                Max = haptic.Value.MaxValue,
                Min = haptic.Value.MinValue,
                ZeroThreshold = haptic.Value.ZeroThreshold
            });
        }
        
        var json = System.Text.Json.JsonSerializer.Serialize(new { AppSettings = appSettings });
        File.WriteAllText("appsettings.json", json);
    }
}
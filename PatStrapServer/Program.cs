using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PatStrapServer.PatStrap;

namespace PatStrapServer;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        IConfigurationBuilder builder1 = new ConfigurationBuilder().AddJsonFile("appsettings.json", false, true);
        IConfigurationRoot appConfiguration = builder1.Build();
        
        Console.WriteLine($"Power={ appConfiguration["power"] }");
        
        var builder = Host.CreateApplicationBuilder(args);

        // builder.Services.AddSingleton<Service>();
        builder.Services.AddSingleton<ServiceLocator>();
        builder.Services.AddSingleton<Osc.Service>();
        builder.Services.AddSingleton<Service>();
        builder.Services.AddHostedService<Service>(p => p.GetRequiredService<Service>());
        builder.Services.AddTransient<IConfiguration>(provider => appConfiguration);
        builder.Services.AddHostedService<Worker>();

        using var host = builder.Build();
        
        await host.RunAsync();
    }
}
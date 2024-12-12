using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PatStrapServer.PatStrap;

namespace PatStrapServer;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // builder.Services.AddSingleton<Service>();
        builder.Services.AddSingleton<ServiceLocator>();
        builder.Services.AddSingleton<Osc.Service>();
        builder.Services.AddSingleton<Service>();
        builder.Services.AddHostedService<Service>(p => p.GetRequiredService<Service>());
        builder.Services.AddHostedService<Worker>();

        using var host = builder.Build();
        
        await host.RunAsync();
    }
}
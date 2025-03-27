using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PatStrapServer.PatStrap;
using Avalonia;
using AvaloniaApplication1;
using Lemon.Hosting.AvaloniauiDesktop;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;

namespace PatStrapServer;

internal static class Program
{
    private static HostApplicationBuilder builder;
    
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();

    public static AppBuilder ConfigAvaloniaAppBuilder(AppBuilder appBuilder)
    {
        IconProvider.Current
            .Register<FontAwesomeIconProvider>();
        
        return appBuilder
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    private static void RunAppWithServiceProvider(HostApplicationBuilder hostBuilder, string[] args) 
    {
        // add avaloniaui application and config AppBuilder
        hostBuilder.Services.AddAvaloniauiDesktopApplication<App>(ConfigAvaloniaAppBuilder);
        
        // add MainWindowViewModelWithParams
        hostBuilder.Services.AddSingleton<MainWindow>();
        
        // build host
        var appHost = hostBuilder.Build();
        // appHost.RunAsync();
        // Todo: remove this cringe
        App.AppHost = appHost;
        
        // run app
        appHost.RunAvaloniauiApplication(args);
    }
    
    [STAThread]
    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [RequiresDynamicCode("Calls Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder()")]
    private static async Task Main(string[] args)
    {
        var appConfiguration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();
        
        builder = Host.CreateApplicationBuilder(args);

        // builder.Services.AddSingleton<Service>();
        builder.Services.AddSingleton<ServiceLocator>();
        builder.Services.AddSingleton<Osc.Service>();
        builder.Services.AddSingleton<Service>();
        builder.Services.AddHostedService<Service>(p => p.GetRequiredService<Service>());
        builder.Services.AddTransient<IConfiguration>(provider => appConfiguration);
        builder.Services.AddHostedService<Worker>();
        RunAppWithServiceProvider(builder, args);

        // using var host = builder.Build();
        //
        // await host.RunAsync();
    }
}
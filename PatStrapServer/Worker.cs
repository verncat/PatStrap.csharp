using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PatStrapServer.PatStrap;
using PatStrapServer.protocol;

namespace PatStrapServer;

public sealed class Worker(ILogger<Worker> logger, 
                            PatStrap.Service patService, 
                            PatStrap.ServiceLocator patServiceLocator, 
                            Osc.Service oscService) : BackgroundService
{
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Worker started!");
            
            patServiceLocator.PatstrapServiceLocated += (sender, serviceInfo) =>
            {
                IProtocol? proto;
                switch (serviceInfo)
                {
                    case PatStrapLegacyServiceInfo:
                        proto = new OriginalProtocol();
                        logger.LogInformation("Found PatStrap with Legacy Protocol!");
                        break;
                    case PatStrapModernServiceInfo:
                        proto = new ModernProtocol();
                        logger.LogInformation("Found PatStrap with Modern Protocol!");
                        break;
                    default:
                        logger.LogCritical($"Not implemented PatStrap service type {serviceInfo.GetType().Name}");
                        // Debug.Assert(false, $"Not implemented PatStrap service type {serviceInfo.GetType().Name}");
                        return;
                }
                _ = patService.ConnectAsync(proto, serviceInfo.IpAddress, serviceInfo.Port);
            };
        
            oscService.OnHapticTrigger += (sender, args) =>
            {
                if (args.ContactName == "pat_left")
                {
                    patService.SetHapticValue(HapticAreaType.LeftEar, args.Value);
                }
                if (args.ContactName == "pat_right")
                {
                    patService.SetHapticValue(HapticAreaType.RightEar, args.Value);
                }
            };

            patServiceLocator.RegisterListeners();
        }
        
        await Task.CompletedTask;
    }
}
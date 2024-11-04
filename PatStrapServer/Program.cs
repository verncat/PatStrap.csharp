using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using BuildSoft.OscCore;
using CoreOSC.IO;
using Makaretu.Dns;
using PatStrapServer.PatStrap;
using PatStrapServer.protocol;
using VRC.OSCQuery;

namespace PatStrapServer;


class Program
{
    static async Task Main(string[] args)
    {
        var oscService = new Osc.Service();
        var patStrapServiceLocator = new PatStrap.ServiceLocator();
        
        PatStrap.BasePatStrapServiceInfo? patstrapServiceInfo = null;
        PatStrap.Service? patstrapService = null;

        patStrapServiceLocator.PatstrapServiceLocated += (sender, serviceInfo) =>
        {
            switch (serviceInfo)
            {
                case PatStrap.PatStrapLegacyServiceInfo:
                    patstrapService = new PatStrap.Service(new OriginalProtocol());
                    Console.WriteLine("Found PatStrap with Legacy Protocol!");
                    break;
                case PatStrap.PatStrapModernServiceInfo:
                    patstrapService = new PatStrap.Service(new ModernProtocol());
                    Console.WriteLine("Found PatStrap with Modern Protocol!");
                    break;
                default:
                    Debug.Assert(false, $"Not implemented PatStrap service type {serviceInfo.GetType().Name}");
                    break;
            }

            patstrapServiceInfo = serviceInfo;
        };
        
        patStrapServiceLocator.Start();
        
        // Register OSCQuery and OSC Udp Server
        oscService.Register();

        oscService.OnHapticTrigger += (sender, args) =>
        {
            if (args.ContactName == "PatLeft")
            {
                patstrapService?.SetHapticValue(HapticAreaType.LeftEar, args.Value);
            }
            if (args.ContactName == "PatRight")
            {
                patstrapService?.SetHapticValue(HapticAreaType.RightEar, args.Value);
            }
        };
        
        while (true)
        {
            if (patstrapService != null)
            {
                // Connect to service if its located
                if (patstrapServiceInfo != null && !patstrapService.IsRunning)
                {
                    await patstrapService.ConnectAsync(patstrapServiceInfo.IpAddress, patstrapServiceInfo.Port);
                }
                
                // Do some work
                await patstrapService.Run();
            }

            // Yield current thread
            if (!Thread.Yield())
                Thread.Sleep(1);
        }
    }
}
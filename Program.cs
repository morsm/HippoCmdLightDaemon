using System;
using System.IO;
using System.Threading;

using Newtonsoft.Json;

namespace Termors.Services.HippoCmdLightDaemon
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            var config = ReadConfig();

            LightService.ShellName = config.Shell;

            // Start web services
            SetupServices(config.Lamps);

            // Run until Ctrl+C
            var endEvent = new ManualResetEvent(false);
            Console.WriteLine("HippoCmdLightDaemon started");

            Console.CancelKeyPress += (sender, e) =>
            {
                Console.WriteLine("HippoCmdLightDaemon stopped");

                LightService.DisposeAll();                  // Stop all web servers

                endEvent.Set();
            };

            endEvent.WaitOne();
        }

        public static Configuration ReadConfig()
        {
            using (StreamReader rea = new StreamReader("cmdlight.json"))
            {
                string json = rea.ReadToEnd();
                return JsonConvert.DeserializeObject<Configuration>(json);
            }
        }

        public static void SetupServices(ConfigObject[] objects)
        {
            foreach (var o in objects)
            {
                var svc = new LightService(o);
                svc.StartWebserver();
                svc.RegisterMDNS();
            }
        }
    }
}

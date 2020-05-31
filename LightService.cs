using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Web.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;

using Makaretu.Dns;
using Microsoft.Owin.Hosting;
using Owin;

namespace Termors.Services.HippoCmdLightDaemon
{
    public delegate Task LampSwitchedDelegate(LightService service, bool on);

    public class LightService : IDisposable
    {
        private readonly ushort _port;
        private IDisposable _webapp = null;
        private bool _on;
        private readonly ConfigObject _config;

        public static string ShellName { get; set; }

        public LightService(ConfigObject obj)
        {
            Name = obj.Name;
            _port = obj.Port;
            _config = obj;
        }

        public static readonly IDictionary<ushort, LightService> Registry = new Dictionary<ushort, LightService>();

        protected static readonly ServiceDiscovery Discovery = new ServiceDiscovery();

        public void RegisterMDNS()
        {
            var service = new ServiceProfile("HippoLed-" + Name, "_hippohttp._tcp", _port);

            Discovery.Advertise(service);
        }

        public void StartWebserver()
        {
            string url = "http://*:" + _port;
            _webapp = WebApp.Start(url, new Action<IAppBuilder>(WebConfiguration));

            Registry[_port] = this;
        }

        public bool On
        {
            get
            {
                _on = ExecuteQueryCommand();
                return _on;
            }
            set
            {
                bool oldStatus = _on;
                if (oldStatus != value)
                {
                    _on = value;
                    if (_on) ExecuteOnCommand(); else ExecuteOffCommand();
                }
            }
        }

        private void ExecuteOffCommand()
        {
            string dummy;
            var success = ExecuteCommandAndReturnOutput(_config.OffCommand, out dummy);

            if (success != 0) throw new Exception("Could not switch lamp off");
        }

        private void ExecuteOnCommand()
        {
            string dummy;
            var success = ExecuteCommandAndReturnOutput(_config.OnCommand, out dummy);

            if (success != 0) throw new Exception("Could not switch lamp on");
        }

        private bool ExecuteQueryCommand()
        {
            string dummy;
            var success = ExecuteCommandAndReturnOutput(_config.QueryCommand, out dummy);

            if (success != 0) throw new Exception("Could not query lamp status");

            return dummy.ToCharArray()[0] == '1';
        }

        private int ExecuteCommandAndReturnOutput(string command, out string stdin)
        {
            int retVal = 1; // error
            stdin = "";

            try
            {
                var psi = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    FileName = ShellName,
                    Arguments = String.Format("-c '{0}'", command)
                };

                var proc = Process.Start(psi);

                stdin = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();
                retVal = proc.ExitCode;
            }
            catch (Exception e)
            {
                retVal = 2;
                Console.WriteLine("Error {0} ({1}) executing command {2}", e.GetType().Name, e.Message, command);
            }

            return retVal;
        }

        public string Name
        {
            get; set;
        }


        public void Dispose()
        {
            if (_webapp != null) _webapp.Dispose();
        }

        public static void DisposeAll()
        {
            foreach (var svc in Registry.Values) svc.Dispose();
            Registry.Clear();
        }


        // This code configures Web API using Owin
        private void WebConfiguration(IAppBuilder appBuilder)
        {
            // Configure Web API for self-host. 
            HttpConfiguration config = new HttpConfiguration();

            // Format to JSON by default
            config.Formatters.Clear();
            config.Formatters.Add(new JsonMediaTypeFormatter());

            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            config.EnsureInitialized();

            appBuilder.UseWebApi(config);
        }

    }
}

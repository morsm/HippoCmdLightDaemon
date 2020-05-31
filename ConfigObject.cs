using System;

namespace Termors.Services.HippoCmdLightDaemon
{
    public class ConfigObject
    {
        public string Name { get; set; }
        public ushort Port { get; set; }
        public string OnCommand { get; set; }
        public string OffCommand { get; set; }
        public string QueryCommand { get; set; }
    }

    public class Configuration
    {
        public string Shell { get; set; }
        public ConfigObject[] Lamps { get; set; }
    }
}

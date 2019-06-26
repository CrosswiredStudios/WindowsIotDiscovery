using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsIotDiscovery.Common.Models
{
    public class DiscoverableDevice
    {
        [JsonProperty(PropertyName = "deviceInfo")]
        public JObject DeviceInfo { get; set; }

        [JsonProperty(PropertyName = "ipAddress")]
        public string IpAddress { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
    }
}

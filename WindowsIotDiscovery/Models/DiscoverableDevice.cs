using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsIotDiscovery.Models
{
    public class DiscoverableDevice
    {
        [JsonProperty(PropertyName = "deviceInfo")]
        public string DeviceInfo { get; set; }

        [JsonProperty(PropertyName = "id")]
        public int Id { get; set; }

        [JsonProperty(PropertyName = "ipAddress")]
        public string IpAddress { get; set; }

        [JsonProperty(PropertyName = "serialNumber")]
        public string SerialNumber { get; set; }
    }
}

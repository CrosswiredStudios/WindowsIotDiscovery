using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace IotDiscoveryClient.Models
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

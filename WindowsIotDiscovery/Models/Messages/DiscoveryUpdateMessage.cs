using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WindowsIotDiscovery.Models.Messages
{
    /// <summary>
    /// A message template for informing the Discovery System Client that we have received their information
    /// </summary>
    public class DiscoveryUpdateMessage
    {
        [JsonProperty(PropertyName = "command")]
        public string Command { get => "UPDATE"; }
        [JsonProperty(PropertyName = "deviceInfo")]
        public JObject DeviceInfo { get; set; }
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        public DiscoveryUpdateMessage(string name, JObject deviceInfo)
        {
            DeviceInfo = deviceInfo;
            Name = name;
        }
    }
}

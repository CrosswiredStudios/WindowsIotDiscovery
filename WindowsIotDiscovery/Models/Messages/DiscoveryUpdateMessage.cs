using Newtonsoft.Json.Linq;

namespace WindowsIotDiscovery.Models.Messages
{
    /// <summary>
    /// A message template for informing the Discovery System Client that we have received their information
    /// </summary>
    public class DiscoveryUpdateMessage
    {
        public string Command => "UPDATE";
        public JObject DeviceInfo { get; set; }
        public string Name { get; set; }

        public DiscoveryUpdateMessage(string name, JObject deviceInfo)
        {
            DeviceInfo = deviceInfo;
            Name = name;
        }
    }
}

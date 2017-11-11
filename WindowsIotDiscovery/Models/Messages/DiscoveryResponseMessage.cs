using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;

namespace WindowsIotDiscovery.Models.Messages
{

    public sealed class DiscoveryResponseMessage
    {    

        JObject deviceInfo;
        string name;
        string silenceUrl;

        /// <summary>
        /// Discovery Response Command
        /// </summary>
        [JsonProperty(PropertyName = "command")]
        public string Command => "IDENTIFY";

        /// <summary>
        /// Discovery Request Command
        /// </summary>
        [JsonProperty(PropertyName = "deviceInfo")]
        public JObject DeviceInfo
        {
            get => deviceInfo;
        }

        /// <summary>
        /// Discovery Request Command
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name
        {
            get => name;
        }

        /// <summary>
        /// Discovery Request Command
        /// </summary>
        [JsonProperty(PropertyName = "silenceUrl")]
        public string SilenceUrl
        {
            get => silenceUrl;
        }

        public DiscoveryResponseMessage(string name, JObject deviceInfo, string silenceUrl)
        {
            this.deviceInfo = deviceInfo;
            this.name = name;
            this.silenceUrl = silenceUrl;
        }
    }
}

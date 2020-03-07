using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace IotDiscoveryClient.Models.Messages
{
    [DataContract]
    public sealed class DiscoveryResponseMessage
    {    

        JObject deviceInfo;
        string name;
        string silenceUrl;

        /// <summary>
        /// Discovery Response Command
        /// </summary>
        [DataMember]
        [JsonProperty(PropertyName = "command")]
        public string Command => "IDENTIFY";

        /// <summary>
        /// Discovery Request Command
        /// </summary>
        [DataMember]
        [JsonProperty(PropertyName = "deviceInfo")]
        public JObject DeviceInfo
        {
            get => deviceInfo;
        }

        /// <summary>
        /// Discovery Request Command
        /// </summary>
        [DataMember]
        [JsonProperty(PropertyName = "name")]
        public string Name
        {
            get => name;
        }

        /// <summary>
        /// Discovery Request Command
        /// </summary>
        [DataMember]
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

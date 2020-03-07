using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IotDiscoveryClient.Models.Messages
{
    public class DiscoveryDataMessage
    {
        /// <summary>
        /// Discovery Response Command
        /// </summary>
        [JsonProperty(PropertyName = "command")]
        public string Command => "DATA";

        /// <summary>
        /// The data payload of the message
        /// </summary>
        [JsonProperty(PropertyName = "data")]
        public JObject Data { get; }

        /// <summary>
        /// The data payload of the message
        /// </summary>
        [JsonProperty(PropertyName = "recipients")]
        public string Recipients { get; }

        public DiscoveryDataMessage(JObject data, string[] recipients = null)
        {
            Data = data;
            if (recipients == null)
                Recipients = "all";
            else
                Recipients = string.Join(",", recipients);
        }
    }
}

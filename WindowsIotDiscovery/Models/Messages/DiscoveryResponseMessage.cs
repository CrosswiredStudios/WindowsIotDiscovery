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

        public DiscoveryResponseMessage(string name, JObject deviceInfo, string silenceUrl)
        {
            this.deviceInfo = deviceInfo;
            this.name = name;
            this.silenceUrl = silenceUrl;
        }
    }
}

using System;

namespace WindowsIotDiscovery.Core
{
    public class DiscoveryClient : WindowsIotDiscovery.Common.Models.DiscoveryClient
    {
        public override void Discover()
        {
            throw new NotImplementedException();
        }

        public override void Initialize(string name, object deviceInfo)
        {
        }
    }
}

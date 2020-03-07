using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using IotDiscoveryClient.Models;

namespace IotDiscoveryClient.Controllers
{
    public sealed class DiscoveryController : WebApiController
    {
        private DiscoveryClient _discoveryClient;

        public DiscoveryController() { }

        public DiscoveryController(DiscoveryClient discoveryClient)
        {
            _discoveryClient = discoveryClient;
        }

        // Gets all records.
        // This will respond to
        //     GET http://localhost:9696/api/people
        [Route(HttpVerbs.Get, "/directMessage/{message}")]
        public bool DirectMessage(string message)
        {
            _discoveryClient.WhenDirectMessage(message);
            return true;
        }
    }
}

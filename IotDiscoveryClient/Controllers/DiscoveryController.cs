using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;

namespace IotDiscoveryClient.Controllers
{
    public sealed class DiscoveryController : WebApiController
    {
        // Gets all records.
        // This will respond to
        //     GET http://localhost:9696/api/people
        [Route(HttpVerbs.Get, "/directMessage/{message}")]
        public Task<bool> DirectMessage()
        {
            
        }
    }
}

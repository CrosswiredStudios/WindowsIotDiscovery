using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;

namespace WindowsIotDiscovery.Models.Messages
{

    public sealed class DiscoveryResponseMessage
    {
        #region Properties        

        private string _error;

        private string _ipAddress;

        private string _potPiDeviceId;

        private string _serialNumber;

        private string _tcpPort;

        [JsonIgnore]
        [JsonProperty(Required = Required.Default)]
        public string Error { get { return _error; } }

        /// <summary>
        /// IP Address of the responding device
        /// </summary>
        public string IpAddress { get { return _ipAddress; } }

        /// <summary>
        /// Tells us if the Discovery Response meets the minimum requirements of being valid. Ignored when going to and from JSON.
        /// </summary>
        [JsonIgnore]
        [JsonProperty(Required = Required.Default)]
        public bool IsValid
        {
            get
            {
                // Make sure our minimum requirements are met
                return !String.IsNullOrEmpty(IpAddress) &&
                       !String.IsNullOrEmpty(SerialNumber) &&
                       !String.IsNullOrEmpty(TcpPort);
            }
        }

        /// <summary>
        /// The PotPi Device Id
        /// </summary>
        public string PotPiDeviceId { get { return _potPiDeviceId; } }

        /// <summary>
        /// Serial Number of the responder
        /// </summary>
        public string SerialNumber { get { return _serialNumber; } }

        /// <summary>
        /// TCP port used to send commands to teh responder
        /// </summary>
        public string TcpPort { get { return _tcpPort; } }

        #endregion

        #region Constructors

        public DiscoveryResponseMessage() { }

        public DiscoveryResponseMessage(string responseString)
        {
            Debug.WriteLine(responseString);
            try
            {
                JObject json = JObject.Parse(responseString);
                _ipAddress = json["IpAddress"].ToString();
                _potPiDeviceId = json["PotPiDeviceId"].ToString();
                _serialNumber = json["SerialNumber"].ToString();
                _tcpPort = json["TcpPort"].ToString();
            }
            catch(Exception ex)
            {
                _error = ex.Message;
            }
        }

        public DiscoveryResponseMessage(string device, string deviceType, string ipAddress, string potPiDeviceId, string serialNumber, string tcpPort)
        {            

            _ipAddress = ipAddress;
            _potPiDeviceId = potPiDeviceId;
            _serialNumber = serialNumber;
            _tcpPort = tcpPort;
        }

        #endregion

    }
}

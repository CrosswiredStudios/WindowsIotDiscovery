using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace IotDiscoveryClient.Models.Messages
{
    /// <summary>
    /// A message template that will cause all instances of Discovery System Client on the network to begin responding with Discovery Response Messages.
    /// 
    /// Sample Discovery Reqeust Messages:
    /// 
    /// {"Command":"DISCOVER","IpAddress":"10.0.0.7", "Device":"PotPiServer", "KnownDevices":[]}
    /// {"Command":"DISCOVER","IpAddress":"10.0.0.7", "Device":"PotPiServer", "KnownDevices":[{"Device":"PotPiPowerBox", "IpAddress":"10.0.0.202", "SerialNumber":"123456"}]}
    /// </summary>
    public class DiscoveryRequestMessage
    {
        #region Properties

        #region Private

        private string _command;
        private string _device;
        private string _error;
        private string _ipAddress;
        private JArray _knownDevices;

        #endregion

        #region Public

        /// <summary>
        /// Discovery Request Command
        /// </summary>
        [JsonProperty(PropertyName = "command")]
        public string Command
        {
            get
            {
                return _command;
            }
        }

        /// <summary>
        /// The type of device making the discovery request
        /// </summary>
        public string Device
        {
            get
            {
                return _device;
            }
        }

        /// <summary>
        /// Any error that occured during instantiation
        /// </summary>
        [JsonIgnore]
        [JsonProperty(Required = Required.Default)]
        public string Error
        {
            get
            {
                return _error;
            }
        }

        /// <summary>
        /// The IP Address of the device making the discovery request
        /// </summary>
        public string IpAddress
        {
            get
            {
                return _ipAddress;
            }
        }

        [JsonIgnore]
        [JsonProperty(Required = Required.Default)]
        public bool IsValid
        {
            get
            {
                return (!String.IsNullOrEmpty(Command));
            }
        }

        /// <summary>
        /// A list of all the other devices that have been found by the device making the request
        /// </summary>
        [JsonProperty(PropertyName = "knownDevices")]
        public JArray KnownDevices
        {
            get
            {
                return _knownDevices;
            }
        }

        #endregion

        #endregion

        #region Constructors

        public DiscoveryRequestMessage()
        {
            _command = "";
            _device = "";
            _error = "";
            _ipAddress = "";
            _knownDevices = new JArray();
        }

        public DiscoveryRequestMessage(string command, string device, string ipAddress, JArray knownDevices)
        {
            _command = command;
            _device = device;
            _error = "";
            _ipAddress = ipAddress;
            _knownDevices = knownDevices;
        }

        public DiscoveryRequestMessage( string requestText )
        {
            try
            {
                JObject jRequest = JObject.Parse(requestText);

                if(jRequest["Command"] == null)
                {
                    _command = "";
                    _device = "";
                    _error = "Not a valid Discovery Request Message. Must include Command, Device, and IpAddress";
                    _ipAddress = "";
                    _knownDevices = new JArray();
                    return;
                }
                else
                { 
                    _command = jRequest["Command"].ToString();
                }

                if (jRequest["Device"] != null)
                {
                    _device = jRequest["Device"].ToString();
                }
                else
                {
                    _device = "";
                }

                _error = "";

                if (jRequest["IpAddress"] != null)
                {
                    _ipAddress = jRequest["IpAddress"].ToString();
                }
                else
                {
                    _ipAddress = "";
                }

                if (jRequest["KnownDevices"] != null)
                {
                    _knownDevices = JArray.FromObject(jRequest["KnownDevices"]);
                }
                else
                {
                    _knownDevices = new JArray();
                }

            }
            catch (Exception ex)
            {
                _command = "";
                _device = "";
                _error = ex.Message;
                _ipAddress = "";
                _knownDevices = new JArray();
            }

        }

        #endregion
    }
}

namespace WindowsIotDiscovery.Models.Messages
{
    /// <summary>
    /// A message template for informing the Discovery System Client that we have received their information
    /// </summary>
    public class DiscoveryAcceptMessage
    {
        #region Properties

        private string _command;
        private string _device;
        private string _ipAddress;

        public string Command
        {
            get
            {
                return _command;
            }
        }

        public string Device
        {
            get
            {
                return _device;
            }
        }

        public string IpAddress
        {
            get
            {
                return _ipAddress;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Create a blank DiscoveryAcceptMessage
        /// </summary>
        public DiscoveryAcceptMessage()
        {
            _command = "";
            _device = "";
            _ipAddress = "";
        }

        /// <summary>
        /// Create a DiscoveryAcceptMessage
        /// </summary>
        /// <param name="command">The command to pass in the accept message</param>
        /// <param name="device">The name of the calling device</param>
        /// <param name="ipAddress">The IP Address of the calling device</param>
        public DiscoveryAcceptMessage(string command, string device, string ipAddress)
        {
            _command = command;
            _device = device;
            _ipAddress = ipAddress;
        }

        #endregion
    }
}

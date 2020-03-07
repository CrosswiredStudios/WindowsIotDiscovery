using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using IotDiscoveryClient.Interfaces;
using IotDiscoveryClient.Models.Messages;
using EmbedIO;
using EmbedIO.WebApi;
using IotDiscoveryClient.Controllers;

namespace IotDiscoveryClient.Models
{
    public class DiscoveryClient : IDiscoveryClient
    {
        #region Events
        public event OnDirectMessageEvent OnDirectMessage;
        #endregion

        #region Fields
        /// <summary>
        /// Flag for creating debug output
        /// </summary>
        protected bool debug = true;

        /// <summary>
        /// A collection of all known devices
        /// </summary>
        protected ObservableCollection<DiscoverableDevice> devices;

        /// <summary>
        /// The name this device will register under
        /// </summary>
        protected string name;

        /// <summary>
        /// The udp socket used to discover new devices
        /// </summary>
        protected UdpClient _socket;

        /// <summary>
        /// The port to use for direct communication
        /// </summary>

        protected int tcpPort;
        /// <summary>
        /// Port to send and receive UDP messages on
        /// </summary>
        protected int udpPort;
        #endregion

        #region Properties
        /// <summary>
        /// Holds the current state of the device
        /// </summary>
        public object DeviceInfo { get; set; }

        /// <summary>
        /// A list of all the devices the Discovery System is aware of
        /// </summary>
        public ObservableCollection<DiscoverableDevice> Devices { get; protected set; }

        /// <summary>
        /// This device's IP Adress.
        /// </summary>
        public virtual string IpAddress { get; }

        /// <summary>
        /// Is the Discovery System broadcasting discovery response messages
        /// </summary>
        public bool IsBroadcasting { get; protected set; }

        /// <summary>
        /// The name of this device
        /// </summary>
        public string Name => name;

        /// <summary>
        /// Port the Discovery System will send and receive messages on
        /// </summary>
        public string Port => udpPort.ToString();
        #endregion

        #region Constructor
        public DiscoveryClient(int tcpPort, int udpPort)
        {
            IsBroadcasting = false;
            Devices = new ObservableCollection<DiscoverableDevice>();
            name = string.Empty;
            _socket = new UdpClient();
            this.tcpPort = tcpPort;
            this.udpPort = udpPort;

            #if DEBUG
                debug = true;
            #endif
        }
        #endregion

        #region Methods
        public void Discover()
        {
            if (debug)
                Debug.WriteLine("Discovery System: Sending Discovery Request");
            try
            {
                
               
                // Include all known devices in the request to minimize traffic (smart devices can use this info to determine if they need to respond)
                JArray jDevices = new JArray();
                foreach (var device in Devices)
                {
                    JObject jDevice = new JObject();
                    jDevice.Add("deviceInfo", device.DeviceInfo);
                    jDevice.Add("name", device.Name);
                    jDevices.Add(jDevice);
                }

                // Create a discovery request message
                DiscoveryRequestMessage discoveryRequestMessage = new DiscoveryRequestMessage("DISCOVER", name, IpAddress, jDevices);

                var requestString = JsonConvert.SerializeObject(discoveryRequestMessage);
                var bytes = Encoding.ASCII.GetBytes(requestString);

                if (debug)
                    Debug.WriteLine($"   >>> {requestString}");

                _socket.Send(bytes, bytes.Length);

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Discovery System Server - Send Discovery Request Failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Initiates the Discovery System Client. 
        /// </summary>
        /// <param name="name">This is the port the system will listen for and broadcast udp packets</param>
        /// <param name="deviceInfo">A JSON object containing all the relevant device info</param>
        public void Initialize(string name, object deviceInfo)
        {
            if(debug)
                Debug.WriteLine($"Discovery System: Initializing {name}");

            try
            {
                // Set the device name
                this.name = name;

                // Set initial variables
                DeviceInfo = deviceInfo;

                // Setup a UDP socket listener
                _socket = new UdpClient(udpPort);
                _socket.BeginReceive(new AsyncCallback(OnUdpPacketReceived), null);

                // Tell the world you exist
                SendDiscoveryResponseMessage();

                // Find out who else is out there
                Discover();

                // Set up the rest API
                InitializeRestApi(tcpPort);

                Debug.WriteLine("Discovery System: Success");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Discovery System: Failure");
                Debug.WriteLine("Reason: " + ex.Message);
            }
        }

        /// <summary>
        /// Creates a rest api endpoint for direct TCP communication.
        /// </summary>
        /// <param name="tcpPort">The tcp port to listen on.</param>
        async void InitializeRestApi(int tcpPort)
        {
            Debug.WriteLine("Initializing Rest Api");

            try
            {
                var server = new WebServer(o => o
                    .WithUrlPrefix($"http://localhost:{tcpPort}")
                    .WithMode(HttpListenerMode.EmbedIO))
                // First, we will configure our web server by adding Modules.
                .WithLocalSessionManager()
                .WithWebApi("/discovery", m => m
                    .WithController(() => new DiscoveryController(this)));
                
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            Debug.WriteLine($"Initializing Rest Api Completed");
            Debug.WriteLine($"http://{IpAddress}:{tcpPort}/discovery");
        }

        /// <summary>
        /// Triggered by the DiscoveryController when a direct message is received.
        /// </summary>
        /// <param name="message">The message that was received.</param>
        public void WhenDirectMessage(string message)
        {
            OnDirectMessage.Invoke(message);
        }

        /// <summary>
        /// Handles any incoming UDP packets.
        /// </summary>
        /// <param name="result"></param>
        void OnUdpPacketReceived(IAsyncResult result)
        {
            try
            {
                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 8000);
                byte[] receiveBytes = _socket.EndReceive(result, ref RemoteIpEndPoint);
                string returnData = Encoding.ASCII.GetString(receiveBytes);
                Debug.WriteLine($"Received udp packet: {returnData}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Unable to listen for UDP packets");
                Debug.WriteLine("Reason: " + ex.Message);
            }
        }

        /// <summary>
        /// Sends a direct message to another device that has already been discovered.
        /// </summary>
        /// <typeparam name="T">The type to cast the response to.</typeparam>
        /// <param name="device">The device to send the message to.</param>
        /// <param name="message">The message to send. Make sure to override the objects ToString method.</param>
        /// <returns></returns>
        public async Task<T> SendDirectMessage<T>(DiscoverableDevice device, object message)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var uri = $"http://{device.IpAddress}:{tcpPort}/discovery/directMessage/{message.ToString()}";
                    var response = await httpClient.GetAsync(uri);
                    response.EnsureSuccessStatusCode();
                    string content = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<T>(content);
                };
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return default(T);
            }
        }

        /// <summary>
        /// Sends an identification message. This should be called when receiving a discover request.
        /// </summary>
        void SendDiscoveryResponseMessage()
        {
            try
            {
                // Create a discovery response message
                var discoveryResponse = new DiscoveryResponseMessage(name, JObject.FromObject(DeviceInfo), "");
                var serializedResponse = JsonConvert.SerializeObject(discoveryResponse);
                var bytes = Encoding.ASCII.GetBytes(serializedResponse);
                _socket.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, udpPort));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DiscoveryClient: Could not complete identification broadcast");
            }
        }
        #endregion
    }
}

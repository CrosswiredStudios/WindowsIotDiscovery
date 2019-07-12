using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using WindowsIotDiscovery.Common.Models;
using WindowsIotDiscovery.Common.Models.Messages;

namespace WindowsIotDiscovery.NET
{
    public class DiscoveryClient : Common.Models.DiscoveryClient
    {
        UdpClient socket;

        public DiscoveryClient(int tcpPort, int udpPort)
        {
            broadcasting = false;
            Devices = new ObservableCollection<DiscoverableDevice>();
            name = string.Empty;
            
            this.tcpPort = tcpPort;
            this.udpPort = udpPort;
        }

        public override void Discover()
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

                socket.Send(bytes, bytes.Length);

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
        public override void Initialize(string name, object deviceInfo)
        {
            Debug.WriteLine($"Discovery System: Initializing {name}");

            try
            {
                // Set the device name
                this.name = name;

                // Set initial variables
                this.deviceInfo = deviceInfo;

                // Setup a UDP socket listener
                socket = new UdpClient(udpPort);
                socket.BeginReceive(new AsyncCallback(OnUdpPacketReceived), null);

                // Tell the world you exist
                SendDiscoveryResponseMessage();

                // Find out who else is out there
                Discover();

                // Set up the rest API
                //InitializeRestApi(tcpPort);

                Debug.WriteLine("Discovery System: Success");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Discovery System: Failure");
                Debug.WriteLine("Reason: " + ex.Message);
            }
        }

        void OnUdpPacketReceived(IAsyncResult result)
        {
            
            try
            {
                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 8000);
                byte[] receiveBytes = socket.EndReceive(result, ref RemoteIpEndPoint);
                string returnData = Encoding.ASCII.GetString(receiveBytes);
                Debug.WriteLine($"Received udp packet: {returnData}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Unable to listen for UDP packets");
                Debug.WriteLine("Reason: " + ex.Message);
            }
        }

        void SendDiscoveryResponseMessage()
        {
            try
            {
                // Create a discovery response message
                var discoveryResponse = new DiscoveryResponseMessage(name, JObject.FromObject(deviceInfo), "");
                var serializedResponse = JsonConvert.SerializeObject(discoveryResponse);
                var bytes = Encoding.ASCII.GetBytes(serializedResponse);
                socket.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Broadcast, udpPort));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DiscoveryClient: Could not complete identification broadcast");
            }
        }
        
    }
}

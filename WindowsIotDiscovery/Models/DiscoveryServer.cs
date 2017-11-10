using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using WindowsIotDiscovery.Models.Messages;

namespace WindowsIotDiscovery.Models
{
    public class DiscoveryServer
    {
        /// <summary>
        /// A timer to periodically find smart devices on the network
        /// </summary>
        private Timer discoverSmartDevicesTimer;

        /// <summary>
        /// A socket to broadcast discovery requests
        /// </summary>
        private DatagramSocket socket;

        /// <summary>
        /// Port to send to and listen for UDP packets from other devices
        /// </summary>
        private string udpPort;

        /// <summary>
        /// A list of all the devices the Discovery System is aware of
        /// </summary>
        public List<DiscoverableDevice> Devices { get; set; }

        /// <summary>
        /// IP Address of the DiscoveryServer
        /// </summary>
        public string IpAddress
        {
            get
            {
                var hosts = NetworkInformation.GetHostNames();
                foreach (var host in hosts)
                {
                    if (host.Type == HostNameType.Ipv4)
                    {
                        return host.DisplayName;
                    }
                }
                return "";
            }
        }

        /// <summary>
        /// Initialize the Discovery System
        /// </summary>
        /// <returns></returns>
        public async void Initialize(string udpPort)
        {
            Debug.WriteLine("Discovery System: Initializing");

            try
            {
                this.udpPort = udpPort;

                // Set the message received function
                socket.MessageReceived += ReceiveDiscoveryResponse;

                // Start the server
                await socket.BindServiceNameAsync(udpPort);

                // Set a timer to discover new devices every minute
                discoverSmartDevicesTimer = new Timer(SendDiscoveryRequest, null, 0, 60000);

                Debug.WriteLine("Discovery System: Success");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Discovery System: Failure");
                Debug.WriteLine("Reason: " + ex.Message);
            }
        }

        /// <summary>
        /// Callback fired when a packet is received on the port.
        /// </summary>
        /// <param name="ds"></param>
        /// <param name="args"></param>
        /// Sample Message {"IpAddress":"10.0.0.202","Product":"PotPiServer","Command":"DiscoveryRequest"}
        /// Sample Message {"IpAddress":"10.0.0.202","Product":"PotPiPowerBox","SerialNumber":"1234-abcd","TcpPort":"215"}
        public async void ReceiveDiscoveryResponse(DatagramSocket ds, DatagramSocketMessageReceivedEventArgs args)
        {
            Debug.WriteLine("Discovery System: Received UDP packet");

            try
            {
                // Get the data from the packet
                var resultStream = args.GetDataStream().AsStreamForRead();
                using (var reader = new StreamReader(resultStream))
                {
                    string discoveryResponseString = await reader.ReadToEndAsync();
                    JObject jDiscoveryResponse = JObject.Parse(discoveryResponseString);

                    // The device must broadcast a brand, model, and serial number
                    if (jDiscoveryResponse["brand"] != null &&
                       jDiscoveryResponse["model"] != null &&
                       jDiscoveryResponse["serialNumber"] != null)
                    {
                        // Create a strongly typed model of this new device
                        var newSmartDevice = new DiscoverableDevice();
                        newSmartDevice.DeviceInfo = JsonConvert.SerializeObject(jDiscoveryResponse);
                        newSmartDevice.IpAddress = args.RemoteAddress.DisplayName;
                        newSmartDevice.SerialNumber = jDiscoveryResponse.Value<string>("serialNumber");

                        // Go through the existing devices
                        foreach (var device in Devices)
                        {
                            // Convert the existing devices info to a JObject 
                            JObject smartDeviceInfo = JObject.Parse(device.DeviceInfo);

                            // If this brand and serial number exist in the system
                            if (smartDeviceInfo.Value<string>("brand") == jDiscoveryResponse.Value<string>("brand") &&
                               smartDeviceInfo.Value<string>("serialNumber") == jDiscoveryResponse.Value<string>("serialNumber"))
                            {
                                // Silence the device to avoid repeat responses
                                SilenceSmartDevice(newSmartDevice.IpAddress + jDiscoveryResponse.Value<string>("discoverySilenceUrl"));

                                // If the IP address has changed
                                if (device.IpAddress != newSmartDevice.IpAddress)
                                {
                                    // Update the smart device in the database
                                    device.IpAddress = newSmartDevice.IpAddress;
                                    return;
                                }
                                else // If its a perfect match
                                {
                                    // Ignore the response
                                    return;
                                }
                            }
                        }

                        // Silence the device to avoid repeat responses
                        SilenceSmartDevice(newSmartDevice.IpAddress + jDiscoveryResponse.Value<string>("discoverySilenceUrl"));

                        // Add it to the database
                        Debug.WriteLine("Added: " + newSmartDevice.DeviceInfo);
                        Devices.Add(newSmartDevice);

                    }
                    else // If the response was not valid
                    {
                        Debug.WriteLine("Discovery System: UDP packet not valid");
                        // Ignore the packet
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Discovery System - Failure: " + ex.Message);
            }
        }

        /// <summary>
        /// Sends a discovery request UDP packet
        /// </summary>
        public async void SendDiscoveryRequest(object state = null)
        {
            Debug.WriteLine("DiscoverSystemServer: Sending Discovery Request");
            try
            {
                // Get an output stream to all IPs on the given port
                using (var stream = await socket.GetOutputStreamAsync(new HostName("255.255.255.255"), udpPort))
                {
                    // Get a data writing stream
                    using (var writer = new DataWriter(stream))
                    {
                        // Include all known devices in the request to minimize traffic (smart devices can use this info to determine if they need to respond)
                        JArray jDevices = new JArray();
                        foreach (var device in Devices)
                        {
                            // Convert the existing device info to a JObject 
                            JObject smartDeviceInfo = JObject.Parse(device.DeviceInfo);

                            JObject jDevice = new JObject();

                            jDevice.Add("brand", smartDeviceInfo.Value<string>("brand"));
                            jDevice.Add("ipAddress", device.IpAddress);
                            jDevice.Add("model", smartDeviceInfo.Value<string>("model"));
                            jDevice.Add("serialNumber", device.SerialNumber);
                            jDevices.Add(jDevice);
                        }

                        // Create a discovery request message
                        DiscoveryRequestMessage discoveryRequestMessage = new DiscoveryRequestMessage("DISCOVER", "Server", IpAddress, jDevices);

                        // Convert the request to a JSON string
                        writer.WriteString(JsonConvert.SerializeObject(discoveryRequestMessage));

                        Debug.WriteLine(JsonConvert.SerializeObject(discoveryRequestMessage));

                        // Send
                        await writer.StoreAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Discovery System Server - Send Discovery Request Failed: " + ex.Message);
            }
        }

        private async void SilenceSmartDevice(string apiUrl)
        {
            Debug.WriteLine("Silencing device: " + apiUrl);
            HttpClient httpClient = new HttpClient();
            HttpResponseMessage response = await httpClient.GetAsync("http://" + apiUrl);
        }
    }
}

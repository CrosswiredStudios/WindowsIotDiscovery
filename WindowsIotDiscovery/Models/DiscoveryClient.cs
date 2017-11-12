using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using WindowsIotDiscovery.Models.Messages;

namespace WindowsIotDiscovery.Models
{
    public class DiscoveryClient : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        #region Properties

        /// <summary>
        /// Flag to indicate if the system is broadcasting discovery responses
        /// </summary>
        bool broadcasting;

        /// <summary>
        /// A JSON object that contains all the information about the device
        /// </summary>
        JObject deviceInfo;

        /// <summary>
        /// The name this device will register under
        /// </summary>
        string name;

        /// <summary>
        /// UDP Socket object
        /// </summary>
        DatagramSocket socket;

        /// <summary>
        /// Port to send and receive UDP messages on
        /// </summary>
        string udpPort;

        Subject<DiscoverableDevice> whenDeviceAdded = new Subject<DiscoverableDevice>();
        Subject<DiscoverableDevice> whenDeviceUpdated = new Subject<DiscoverableDevice>();
        

        public JObject DeviceInfo
        {
            get => deviceInfo;
            set { deviceInfo = value; }
        }

        /// <summary>
        /// A list of all the devices the Discovery System is aware of
        /// </summary>
        public ObservableCollection<DiscoverableDevice> Devices { get; set; }

        /// <summary>
        /// The IpAddress of the device
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
        /// Is the Discovery System broadcasting discovery response messages
        /// </summary>
        public bool IsBroadcasting
        {
            get
            {
                return broadcasting;
            }
        }

        /// <summary>
        /// Port the Discovery System will send and receive messages on
        /// </summary>
        public string Port
        {
            get
            {
                return udpPort;
            }
        }

        public IObservable<DiscoverableDevice> WhenDeviceAdded => whenDeviceAdded;
        public IObservable<DiscoverableDevice> WhenDeviceUpdated => whenDeviceUpdated;

        #endregion

        #region Constructors

        /// <summary>
        /// Create a Discovery System Client instance
        /// </summary>
        public DiscoveryClient(string udpPort)
        {
            broadcasting = false;
            Devices = new ObservableCollection<DiscoverableDevice>();
            socket = new DatagramSocket();
            this.udpPort = udpPort;
        }

        #endregion

        #region Methods

        public async void BroadcastUpdate()
        {
            // Get an output stream to all IPs on the given port
            using (var stream = await socket.GetOutputStreamAsync(new HostName("255.255.255.255"), udpPort))
            {
                // Get a data writing stream
                using (var writer = new DataWriter(stream))
                {
                    // Create a discovery response message
                    var discoveryUpdate = new DiscoveryUpdateMessage(name, deviceInfo);

                    var discoveryUpdateString = JsonConvert.SerializeObject(discoveryUpdate);

                    // Convert the request to a JSON string
                    writer.WriteString(discoveryUpdateString);

                    Debug.WriteLine($"   >>> {discoveryUpdateString}");

                    // Send
                    await writer.StoreAsync();
                }
            }
        }

        public async void Discover()
        {
            Debug.WriteLine("Discovery System: Sending Discovery Request");
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
                            JObject jDevice = new JObject();
                            jDevice.Add("deviceInfo", device.DeviceInfo);
                            jDevice.Add("name", device.Name);
                            jDevices.Add(jDevice);
                        }

                        // Create a discovery request message
                        DiscoveryRequestMessage discoveryRequestMessage = new DiscoveryRequestMessage("DISCOVER", "Server", IpAddress, jDevices);

                        // Convert the request to a JSON string
                        writer.WriteString(JsonConvert.SerializeObject(discoveryRequestMessage));

                        Debug.WriteLine($"   >>> {JsonConvert.SerializeObject(discoveryRequestMessage)}");

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

        /// <summary>
        /// Initiates the Discovery System Client. 
        /// </summary>
        /// <param name="udpPort">This is the port the system will listen for and broadcast udp packets</param>
        /// <param name="deviceInfo">A JSON object containing all the relevant device info</param>
        public async void Initialize(string name)
        {
            Debug.WriteLine("Discovery System: Initializing");

            try
            {
                // Set the device name
                this.name = name;

                // Set initial variables
                this.deviceInfo = new JObject();

                // Setup a UDP socket listener
                socket.MessageReceived += ReceivedDiscoveryMessage;
                await socket.BindServiceNameAsync(this.udpPort);
                Debug.WriteLine("Discovery System: Success");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Discovery System: Failure");
                Debug.WriteLine("Reason: " + ex.Message);
            }
        }

        /// <summary>
        /// Asynchronously handle receiving a UDP packet
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="eventArguments"></param>
        private async void ReceivedDiscoveryMessage(DatagramSocket socket, DatagramSocketMessageReceivedEventArgs args)
        {
            
            try
            {
                // Get the data from the packet
                var result = args.GetDataStream();
                var resultStream = result.AsStreamForRead();
                using (var reader = new StreamReader(resultStream))
                {
                    // Load the raw data into a response object
                    var potentialRequestString = (await reader.ReadToEndAsync());

                    if (args.RemoteAddress.DisplayName != IpAddress)
                    {
                        Debug.WriteLine("Discovery System: Received UDP packet");
                        Debug.WriteLine($"   >>> {potentialRequestString}");
                    }

                    JObject jRequest = JObject.Parse(potentialRequestString);
                    
                    // If the message was a valid request
                    if (jRequest["command"] != null)
                    {
                        switch(jRequest.Value<string>("command").ToLower())
                        {
                            case "discover":

                                // If we initiated this discovery request
                                if(args.RemoteAddress.DisplayName == IpAddress)
                                {
                                    // Ignore it
                                    return;
                                }

                                // If the requestor included a list of its known devices
                                if (jRequest["knownDevices"] != null)
                                {
                                    // Go through each device
                                    foreach (var device in jRequest["knownDevices"])
                                    {
                                        // If we are already registered with the requestor
                                        if (device.Value<string>("name") == name)
                                            // Ignore
                                            return;
                                    }
                                }

                                // Begin Broadcasting a discovery response until we get an acceptance or reach the timeout.
                                StartBroadcasting();
                                break;
                            case "identify":
                                // The device must broadcast a name and its device info
                                if (jRequest["name"] != null &&
                                   jRequest["deviceInfo"] != null)
                                {
                                    // Create a strongly typed model of this new device
                                    var newDevice = new DiscoverableDevice();
                                    newDevice.DeviceInfo = jRequest.Value<JObject>("deviceInfo");
                                    newDevice.Name = jRequest.Value<string>("name");
                                    newDevice.IpAddress = args.RemoteAddress.DisplayName;

                                    // Go through the existing devices
                                    foreach (var device in Devices)
                                    {
                                        if (device.Name == newDevice.Name)
                                        {
                                            // If the IP address has changed
                                            if (device.IpAddress != newDevice.IpAddress)
                                            {
                                                // Update the smart device in the database
                                                device.IpAddress = newDevice.IpAddress;

                                                return;
                                            }
                                            else // If its a perfect match
                                            {
                                                // Ignore the response
                                                return;
                                            }
                                        }
                                    }

                                    // Add it to the database
                                    Debug.WriteLine($"Discovery System: Added {newDevice.Name} @ {newDevice.IpAddress}");
                                    Devices.Add(newDevice);
                                    whenDeviceAdded.OnNext(newDevice);
                                }
                                break;
                            case "update":
                                // The device must broadcast a name and its device info
                                if (jRequest["name"] != null &&
                                   jRequest["deviceInfo"] != null)
                                {
                                    // Create a strongly typed model of this new device
                                    var newDevice = new DiscoverableDevice();
                                    newDevice.DeviceInfo = jRequest.Value<JObject>("deviceInfo");
                                    newDevice.Name = jRequest.Value<string>("name");
                                    newDevice.IpAddress = args.RemoteAddress.DisplayName;

                                    // Go through the existing devices
                                    foreach (var device in Devices)
                                    {
                                        // If we find a match
                                        if (device.Name == newDevice.Name)
                                        {
                                            // Update the device info
                                            device.DeviceInfo = newDevice.DeviceInfo;
                                            // Update the Ip Address
                                            device.IpAddress = newDevice.IpAddress;
                                            whenDeviceUpdated.OnNext(device);
                                            // Bounce out!
                                            return;
                                        }
                                    }

                                    // If no matches were found, add this device
                                    Debug.WriteLine($"Discovery System: Added {newDevice.Name} @ {newDevice.IpAddress}");
                                    Devices.Add(newDevice);
                                    whenDeviceAdded.OnNext(newDevice);
                                }
                                break;
                        };
                    }
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"Discovery System: {ex}");
                return;
            }
        }

        /// <summary>
        /// Start broadcasting discovery response messages
        /// </summary>
        public async void StartBroadcasting()
        {
            broadcasting = true;
            int count = 0;

            while (broadcasting)
            {
                // Get an output stream to all IPs on the given port
                using (var stream = await socket.GetOutputStreamAsync(new HostName("255.255.255.255"), udpPort))
                {
                    // Get a data writing stream
                    using (var writer = new DataWriter(stream))
                    {
                        // Create a discovery response message
                        var discoveryResponse = new DiscoveryResponseMessage(name, deviceInfo, "");

                        var discoveryResponseString = JsonConvert.SerializeObject(discoveryResponse);

                        // Convert the request to a JSON string
                        writer.WriteString(discoveryResponseString);

                        Debug.WriteLine($"   >>> {discoveryResponseString}");

                        // Send
                        await writer.StoreAsync();
                    }
                }


                // Enforce maximum of 10 seconds of broadcasting
                count++;
                if (count == 10) broadcasting = false;
                await Task.Delay(2000);
            }
        }

        /// <summary>
        /// Stops the Discovery System Client from broadcasting response messages.
        /// </summary>
        public void StopBroadcasting()
        {
            Debug.WriteLine("Discovery System Client: Stopping Discovery Response broadcast");
            broadcasting = false;
        }

        #endregion
    }
}

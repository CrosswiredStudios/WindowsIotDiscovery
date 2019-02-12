using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Restup.Webserver.Attributes;
using Restup.Webserver.Http;
using Restup.Webserver.Models.Contracts;
using Restup.Webserver.Models.Schemas;
using Restup.Webserver.Rest;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Core;
using WindowsIotDiscovery.Models.Messages;

namespace WindowsIotDiscovery.Models
{
    public class DiscoveryClient : INotifyPropertyChanged
    {
        const bool debug = true;

        public event PropertyChangedEventHandler PropertyChanged;

        #region Properties

        /// <summary>
        /// Flag to indicate if the system is broadcasting discovery responses
        /// </summary>
        bool broadcasting;
        /// <summary>
        /// A JSON object that contains all the information about the device
        /// </summary>
        object deviceInfo;
        ObservableCollection<DiscoverableDevice> devices;
        /// <summary>
        /// The name this device will register under
        /// </summary>
        string name;
        /// <summary>
        /// UDP Socket object
        /// </summary>
        DatagramSocket socket;
        int tcpPort;
        /// <summary>
        /// Port to send and receive UDP messages on
        /// </summary>
        int udpPort;

        readonly Subject<JObject> whenDataReceived = new Subject<JObject>();
        readonly Subject<string> whenDirectMessage = new Subject<string>();
        readonly Subject<DiscoverableDevice> whenDeviceAdded = new Subject<DiscoverableDevice>();
        readonly Subject<DiscoverableDevice> whenDeviceUpdated = new Subject<DiscoverableDevice>();
        readonly Subject<JObject> whenUpdateReceived = new Subject<JObject>();

        /// <summary>
        /// Holds the current state of the device
        /// </summary>
        public object DeviceInfo
        {
            get => deviceInfo;
            set { deviceInfo = value; }
        }

        /// <summary>
        /// A list of all the devices the Discovery System is aware of
        /// </summary>
        public ObservableCollection<DiscoverableDevice> Devices
        {
            get => devices;
            set
            {
                devices = value;
            }
        }

        public bool HasErrors { get; set; }

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
        public bool IsBroadcasting => broadcasting;

        public string Name => name;

        /// <summary>
        /// Port the Discovery System will send and receive messages on
        /// </summary>
        public string Port => udpPort.ToString();

        public IObservable<DiscoverableDevice> WhenDeviceAdded => whenDeviceAdded;
        public IObservable<JObject> WhenDataReceived => whenDataReceived;
        public IObservable<DiscoverableDevice> WhenDeviceUpdated => whenDeviceUpdated;
        public IObservable<string> WhenDirectMessage => whenDirectMessage;
        public IObservable<JObject> WhenUpdateReceived => whenUpdateReceived;

        #endregion

        #region Constructors

        /// <summary>
        /// Create a Discovery System Client instance
        /// </summary>
        public DiscoveryClient(int tcpPort, int udpPort)
        {
            broadcasting = false;
            Devices = new ObservableCollection<DiscoverableDevice>();
            name = string.Empty;
            socket = new DatagramSocket();
            this.tcpPort = tcpPort;
            this.udpPort = udpPort;
        }

        #endregion

        #region Methods

        public async void BroadcastUpdate(JObject currentDeviceInfo = null)
        {
            // Update device info with passed in information
            if (currentDeviceInfo != null) deviceInfo = currentDeviceInfo;

            // Get an output stream to all IPs on the given port
            using (var stream = await socket.GetOutputStreamAsync(new HostName("255.255.255.255"), udpPort.ToString()))
            {
                // Get a data writing stream
                using (var writer = new DataWriter(stream))
                {
                    // Create a discovery update message
                    var discoveryUpdate = new DiscoveryUpdateMessage(name, JObject.FromObject(deviceInfo));

                    // Convert to a JSON string
                    var discoveryUpdateString = JsonConvert.SerializeObject(discoveryUpdate);

                    // Send it
                    writer.WriteString(discoveryUpdateString);

                    if(debug)
                        Debug.WriteLine($"   >>> {discoveryUpdateString}");

                    // Send
                    await writer.StoreAsync();
                }
            }
        }

        public async void Discover()
        {
            if (debug)
                Debug.WriteLine("Discovery System: Sending Discovery Request");
            try
            {
                // Get an output stream to all IPs on the given port
                using (var stream = await socket.GetOutputStreamAsync(new HostName("255.255.255.255"), udpPort.ToString()))
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
                        DiscoveryRequestMessage discoveryRequestMessage = new DiscoveryRequestMessage("DISCOVER", name, IpAddress, jDevices);

                        // Convert the request to a JSON string
                        writer.WriteString(JsonConvert.SerializeObject(discoveryRequestMessage));

                        if (debug)
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

        public async Task<TResult> GetDeviceState<TResult>(DiscoverableDevice device)
        {
            using (var httpClient = new HttpClient())
            {
                var uri = new Uri($"http://{device.IpAddress}:{tcpPort}/windowsIotDiscovery/state");
                var response = await httpClient.GetAsync(uri);
                var responseStream = await response.Content.ReadAsStreamAsync();

                using (var reader = new StreamReader(responseStream))
                {
                    using (var jsonReader = new JsonTextReader(reader))
                    {
                        return new JsonSerializer().Deserialize<TResult>(jsonReader);
                    }
                }
                
            }
            
        }

        /// <summary>
        /// Initiates the Discovery System Client. 
        /// </summary>
        /// <param name="udpPort">This is the port the system will listen for and broadcast udp packets</param>
        /// <param name="deviceInfo">A JSON object containing all the relevant device info</param>
        public async void Initialize(string name, object deviceInfo)
        {
            Debug.WriteLine($"Discovery System: Initializing {name}");

            try
            {
                // Set the device name
                this.name = name;

                // Set initial variables
                this.deviceInfo = deviceInfo;

                // Setup a UDP socket listener
                socket.MessageReceived += ReceivedDiscoveryMessage;
                await socket.BindServiceNameAsync(udpPort.ToString());

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
                var restRouteHandler = new RestRouteHandler();
                restRouteHandler.RegisterController<DiscoveryController>(this);

                var configuration = new HttpServerConfiguration()
                  .ListenOnPort(tcpPort)
                  .RegisterRoute("discovery", restRouteHandler)
                  .EnableCors();

                var httpServer = new HttpServer(configuration);
                await httpServer.StartServerAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            Debug.WriteLine($"Initializing Rest Api Completed");
            Debug.WriteLine($"http://{IpAddress}:{tcpPort}/discovery");
        }

        internal void OnDirectMessage(string message)
        {
            whenDirectMessage.OnNext(message);
        }

        /// <summary>
        /// Asynchronously handle receiving a UDP packet
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="eventArguments"></param>
        async void ReceivedDiscoveryMessage(DatagramSocket socket, DatagramSocketMessageReceivedEventArgs args)
        {
            try
            {
                // Get the data from the packet
                var result = args.GetDataStream();
                var resultStream = result.AsStreamForRead();
                using (var reader = new StreamReader(resultStream))
                {
                    // Load the raw data into a response object
                    var potentialRequestString = await reader.ReadToEndAsync();

                    // Ignore messages from yourself
                    if (args.RemoteAddress.DisplayName != IpAddress)
                    {
                        if (debug)
                        {
                            Debug.WriteLine("Discovery System: Received UDP packet");
                            Debug.WriteLine($"   >>> {potentialRequestString}");
                        }
                    }
                    else
                    {
                        return;
                    }

                    JObject jRequest = JObject.Parse(potentialRequestString);
                    
                    // If the message was a valid request
                    if (jRequest["command"] != null)
                    {
                        switch(jRequest.Value<string>("command").ToLower())
                        {
                            case "data":
                                // Get the intended recipients
                                var intendedRecipients = jRequest.Value<string>("recipients").Split(',');
                                // If I am in the list or it is for everyone
                                if (intendedRecipients.Any(r=>r == "all" || r == name))
                                {
                                    // Fire off a data event with the data payload
                                    whenDataReceived.OnNext(JObject.FromObject(jRequest["data"]));
                                }
                                break;
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

                                    // Add it to the database
                                    if (debug)
                                        Debug.WriteLine($"Discovery System: Added {newDevice.Name} @ {newDevice.IpAddress}");

                                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                                    {
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
                                        Devices.Add(newDevice);
                                        whenDeviceAdded.OnNext(newDevice);
                                    });
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
                                    if (debug)
                                        Debug.WriteLine($"Discovery System: Added {newDevice.Name} @ {newDevice.IpAddress}");

                                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                                    {
                                        Devices.Add(newDevice);
                                        whenDeviceAdded.OnNext(newDevice);
                                    });
                                    
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

        public async void SendDataMessage(DiscoveryDataMessage discoveryDataMessage)
        {
            Debug.WriteLine($"DiscoveryClient: Sending data message.");
            try
            {
                // Get an output stream to all IPs on the given port
                using (var stream = await socket.GetOutputStreamAsync(new HostName("255.255.255.255"), udpPort.ToString()))
                {
                    // Get a data writing stream
                    using (var writer = new DataWriter(stream))
                    {
                        // Get the data message as a string
                        var dataMessageString = JsonConvert.SerializeObject(discoveryDataMessage);

                        // Write the string to the stream
                        writer.WriteString(dataMessageString);

                        if (debug)
                            Debug.WriteLine($"   >>> {dataMessageString}");

                        // Send
                        await writer.StoreAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DiscoveryClient: Could not send data message.");
            }
        }

        public async Task<bool> SendDirectMessage(string deviceName, string message)
        {
            try
            {
                var device = Devices.FirstOrDefault(d => d.Name == deviceName);
                using (var httpClient = new HttpClient())
                {
                    var uri = $"http://{device.IpAddress}:{tcpPort}/discovery/directMessage/{message}";
                    var response = await httpClient.GetAsync(uri);
                    return response.IsSuccessStatusCode;
                };
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex);
                return false;
            }
        }

        async void SendDiscoveryResponseMessage()
        {
            try
            {
                // Get an output stream to all IPs on the given port
                using (var stream = await socket.GetOutputStreamAsync(new HostName("255.255.255.255"), udpPort.ToString()))
                {
                    // Get a data writing stream
                    using (var writer = new DataWriter(stream))
                    {
                        // Create a discovery response message
                        var discoveryResponse = new DiscoveryResponseMessage(name, JObject.FromObject(deviceInfo), "");

                        var discoveryResponseString = JsonConvert.SerializeObject(discoveryResponse);

                        // Convert the request to a JSON string
                        writer.WriteString(discoveryResponseString);

                        if (debug)
                            Debug.WriteLine($"   >>> {discoveryResponseString}");

                        // Send
                        await writer.StoreAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"DiscoveryClient: Could not complete identification broadcast");
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
                SendDiscoveryResponseMessage();

                // Enforce maximum of 10 seconds of broadcasting
                count++;
                if (count == 5) broadcasting = false;
                await Task.Delay(2000);
            }
        }

        /// <summary>
        /// Stops the Discovery System Client from broadcasting response messages.
        /// </summary>
        public void StopBroadcasting()
        {
            if (debug)
                Debug.WriteLine("Discovery System Client: Stopping Discovery Response broadcast");
            broadcasting = false;
        }

        #endregion
    }

    [RestController(InstanceCreationType.Singleton)]
    public class DiscoveryController
    {
        public DiscoveryClient discoveryClient;

        public DiscoveryController(object discoveryClient)
        {
            this.discoveryClient = discoveryClient as DiscoveryClient;
        }        

        [UriFormat("/directMessage/{message}")]
        public IGetResponse DirectMessage(string message)
        {
            try
            {
                discoveryClient.OnDirectMessage(message);
                return new GetResponse(
                  GetResponse.ResponseStatus.OK);
            }
            catch (Exception ex)
            {
                return new GetResponse(
                  GetResponse.ResponseStatus.NotFound, ex);
            }
        }

        [UriFormat("/state")]
        public IGetResponse State()
        {
            try
            {
                return new GetResponse(
                  GetResponse.ResponseStatus.OK,
                  "");
            }
            catch (Exception ex)
            {
                return new GetResponse(
                  GetResponse.ResponseStatus.NotFound, ex);
            }
        }

        [UriFormat("/update")]
        public IPostResponse Update([FromContent]JObject payload)
        {
            try
            {
                //whenUpdateReceived.OnNext(payload);
                return new PostResponse(
                  PostResponse.ResponseStatus.Created);
            }
            catch (Exception ex)
            {
                return new PostResponse(
                  PostResponse.ResponseStatus.Conflict, null, ex);
            }
            
        }
    }
}

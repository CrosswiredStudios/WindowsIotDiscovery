using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace WindowsIotDiscovery.Common.Models
{
    public interface IDiscoveryClient
    {
        object DeviceInfo { get; set; }
        ObservableCollection<DiscoverableDevice> Devices { get; }
        string IpAddress { get; }
        string Name { get; }

        void Discover();

        /// <summary>
        /// Initiates the Discovery System Client. 
        /// </summary>
        /// <param name="name">This is the port the system will listen for and broadcast udp packets</param>
        /// <param name="deviceInfo">A JSON object containing all the relevant device info</param>
        void Initialize(string name, object deviceInfo);
    }

    public abstract class DiscoveryClient : IDiscoveryClient
    {
        /// <summary>
        /// Flag to indicate if the system is broadcasting discovery responses
        /// </summary>
        protected bool broadcasting;
        /// <summary>
        /// Flag for creating debug output
        /// </summary>
        protected bool debug = true;
        /// <summary>
        /// A JSON object that contains all the information about the device
        /// </summary>
        protected object deviceInfo;
        /// <summary>
        /// A collection of all known devices
        /// </summary>
        protected ObservableCollection<DiscoverableDevice> devices;
        /// <summary>
        /// The name this device will register under
        /// </summary>
        protected string name;
        /// <summary>
        /// The port to use for direct communication
        /// </summary>
        protected int tcpPort;
        /// <summary>
        /// Port to send and receive UDP messages on
        /// </summary>
        protected int udpPort;

        protected readonly Subject<JObject> whenDataReceived = new Subject<JObject>();
        protected readonly Subject<string> whenDirectMessage = new Subject<string>();
        protected readonly Subject<DiscoverableDevice> whenDeviceAdded = new Subject<DiscoverableDevice>();
        protected readonly Subject<DiscoverableDevice> whenDeviceUpdated = new Subject<DiscoverableDevice>();
        protected readonly Subject<JObject> whenUpdateReceived = new Subject<JObject>();

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

        /// <summary>
        /// Is the Discovery System broadcasting discovery response messages
        /// </summary>
        public bool IsBroadcasting => broadcasting;

        /// <summary>
        /// Port the Discovery System will send and receive messages on
        /// </summary>
        public string Port => udpPort.ToString();


        public virtual string IpAddress { get; }

        public string Name => name;

        public IObservable<DiscoverableDevice> WhenDeviceAdded => whenDeviceAdded;
        public IObservable<JObject> WhenDataReceived => whenDataReceived;
        public IObservable<DiscoverableDevice> WhenDeviceUpdated => whenDeviceUpdated;
        public IObservable<string> WhenDirectMessage => whenDirectMessage;
        public IObservable<JObject> WhenUpdateReceived => whenUpdateReceived;

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

        public abstract void Discover();
        /// <summary>
        /// Initiates the Discovery System Client. 
        /// </summary>
        /// <param name="name">This is the port the system will listen for and broadcast udp packets</param>
        /// <param name="deviceInfo">A JSON object containing all the relevant device info</param>
        public abstract void Initialize(string name, object deviceInfo);
    }
}

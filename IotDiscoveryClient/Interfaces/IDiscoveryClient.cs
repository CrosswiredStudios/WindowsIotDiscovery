using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using IotDiscoveryClient.Models;

namespace IotDiscoveryClient.Interfaces
{
    public delegate void OnDirectMessageEvent(string message);

    public interface IDiscoveryClient
    {
        event OnDirectMessageEvent OnDirectMessage;

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

        /// <summary>
        /// Sends a message to another device over TCP.
        /// </summary>
        /// <typeparam name="T">The type of response expected.</typeparam>
        /// <param name="device">The device to send the message to.</param>
        /// <param name="message">The message to send. Classes will be serialized into JSON objects.</param>
        /// <returns></returns>
        Task<T> SendDirectMessage<T>(DiscoverableDevice device, object message);
    }
}

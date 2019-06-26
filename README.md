# WindowsIotDiscovery
### A simple discovery and communication system for IoT devices on a network.

## Platform

This library is intended for use with any Universal Windows Platform (UWP) app that needs to communicate with other apps. I use it extensively in my personal Windows IoT apps running on Raspberry Pis.

## How to Use

### Initiation

The DiscoveryClient can be added to any app with just two lines:

    var discoveryClient = new DiscoveryClient("1234", "4567");
    discoveryClient.Initialize("My Unique Name", serializableStateObject);

The first line will create an instance of the client with the desired TCP and UDP ports to operate on. The second line sets the unique name of this device and a pointer to an object that holds all state information for the device. It is recommended that you use a JObject. Upon initiation, the device will send out an IDENTIFY packet and a DISCOVER packet to find all the other devices on the network.

### Devices

Once the client is initialized, you can access devices by their name using LINQ:

    var iotDevice = discoveryClient.Devices.FirstOrDefault(device => device.Name == "iotDeviceName");
    
Once you have an object, you have access to the Name, IpAdress, and DeviceInfo - a object that has any information the device provided. 
    
    string iotDeviceIpAddress = iotDevice.IpAddress;
    string iotDeviceName = iotDevice.Name;
    JObject iotDeviceInfo = iotDevice.DeviceInfo;
    
    Debug.WriteLine($"{iotDevice.Name} at {iotDevice.IpAddress} has a state of {iotDevice.DeviceInfo.GetValue<string>("state")}");

### Discovery

Since the client identifies on start up and attempts to discover other devices, everything should just work :) but because we live in the real world you may want to periodically send a discovery request to make sure you are aware of all other devices. You can do that with the Discover() function.

    var discoveryTimer = new Timer((state) =>
    {
        discoveryClient.Discover();
    }, null, 0, 30000);

*Calling Discover() broadcasts a UDP message with a list of known devices. Each device will look at the list and respond if they are not present or if their IP address has changed. This also means that device names must be unique.*

### Events

Sometimes you will want to know when a device is added or when a device's state is updated. The client utilizes System.Reactive observables that you can subscribe to:

**WhenDeviceAdded**

    using System.Reactive.Linq;
    using System.Threading;
    
    var whenDeviceAdded = discoveryClient
      .WhenDeviceAdded
      .ObserveOn(SynchronizationContext.Current)
      .Subscribe(device =>
      {
         Debug.WriteLine("A device was added");
      });

**WhenDeviceUpdated**

    using System.Reactive.Linq;
    using System.Threading;
    
    var whenDeviceUpdated = discoveryClient
      .WhenDeviceUpdated
      .ObserveOn(SynchronizationContext.Current)
      .Subscribe(device =>
      {
         Debug.WriteLine("A device was updated");
      });
  
*ObserveOn(SynchronizationContext.Current) is used to ensure that the code runs on the UI thread.*
*Dont forget to dispose of subscriptions when you're done ;)*

### Direct Messaging (TCP)

Once a device is discovered, it is possible to send messages directly to that device. This is done utilizing TCP to ensure that the message is delivered. Each device runs a lightweight rest api that listens for incoming messages. A message can be as simple as a character but it is more useful to send a serialized object.

**SendDirectMessage**
    
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    var jMessage = new JObject();
    jMessage.Add("powerLevel", 100);
    
    var success = await discoveryClient.SendDirectMessage("myDevice", jMessage.ToString());
    
**WhenDirectMessage**

    using System.Reactive.Linq;
    using System.Threading;
    
    var whenDirectMessage = discoveryClient
        .WhenDirectMessage
        .ObserveOn(SynchronizationContext.Current)
        .Subscribe(message => {
            var jMessage = JObject.Parse(message);
            var powerLevel = message.GetValue<int>("powerLevel");
        });



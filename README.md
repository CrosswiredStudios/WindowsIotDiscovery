# WindowsIotDiscovery
###A simple discovery and communication system for IoT devices on a network.

##Platform

This library is intended for use with any Universal Windows Platform (UWP) app that needs to communicate with other apps. I use it extensively in my personal Windows IoT apps running on Raspberry Pis.

##How to Use

###Initiation

The DiscoveryClient can be added to any app with just two lines:

    var discoveryClient = new DiscoveryClient("1234");
    discoveryClient.Initialize("My Unique Name");

The first line will create an instance of the client with the port to operate on. The second line sets the unique name of this device. Upon initiation, the device will send out an IDENTIFY packet and a DISCOVER packet to find all the other devices on the network.

###Discovery

Since the client identifies on start up and attempts to discover other devices, everything should just work :) but because we live in the real world you may want to periodically send a discovery request to make sure you are aware of all other devices. You can do that with the Discover() function.

    var discoveryTimer = new Timer((state) =>
    {
        discoveryClient.Discover();
    }, null, 0, 30000);

*Calling Discover() broadcasts a UDP message with a list of known devices. Each device will look at the list and respond if they are not present or if their IP address has changed.*

###Events

Sometimes you will want to know when a device is added or when a device's state is updated. The client utilizes System.Reactive observables that you can subscribe to:

**WhenDeviceAdded**

    discoveryClient
      .WhenDeviceAdded
      .ObserveOn(SynchronizationContext.Current)
      .Subscribe(device =>
      {
         Debug.WriteLine("A device was added");
      });

**WhenDeviceUpdated**

    discoveryClient
      .WhenDeviceUpdated
      .ObserveOn(SynchronizationContext.Current)
      .Subscribe(device =>
      {
         Debug.WriteLine("A device was updated");
      });
  
*ObserveOn(SynchronizationContext.Current) is used to ensure that the code runs on the UI thread.*

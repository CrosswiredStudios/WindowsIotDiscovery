# WindowsIotDiscovery
UWP iot discovery system over UDP

This is an easy to use class for making any UWP apps aware of other apps running on the same network.

The DiscoveryClient can be added to any app with just three lines:

var discoveryClient = new DiscoveryClient("1234");
discoveryClient.Initialize("My Unique Name");
discoveryClient.Discover();

Often you will want to periodically check for new devices. Using a timer is the simplest way to do this:

var discoveryTimer = new Timer((state) =>
{
    discoveryClient.Discover();
}, null, 0, 30000);

There are also two observables that can be used to react when a device is added or updated:

discoveryClient
  .WhenDeviceAdded
  .ObserveOn(SynchronizationContext.Current)
  .Subscribe(device =>
  {
     Debug.WriteLine("A device was added");
  });
  
discoveryClient
  .WhenDeviceUpdated
  .ObserveOn(SynchronizationContext.Current)
  .Subscribe(device =>
  {
     Debug.WriteLine("A device was added");
  });
  
  *** ObserveOn() is used to ensure that the code runs on the UI thread.

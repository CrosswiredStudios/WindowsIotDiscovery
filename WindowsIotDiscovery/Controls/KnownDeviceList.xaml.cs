using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using WindowsIotDiscovery.Models;

namespace WindowsIotDiscovery.Controls
{
    public sealed partial class KnownDeviceList : UserControl
    {
        public static readonly DependencyProperty DiscoveryClientProperty =
            DependencyProperty.Register(
                "DiscoveryClient", typeof(DiscoveryClient),
                typeof(KnownDeviceList), null);

        public DiscoveryClient DiscoveryClient
        {
            get { return (DiscoveryClient)GetValue(DiscoveryClientProperty); }
            set
            {
                SetValue(DiscoveryClientProperty, value);                
            }
        }

        public KnownDeviceList()
        {
            InitializeComponent();
        }
    }
}

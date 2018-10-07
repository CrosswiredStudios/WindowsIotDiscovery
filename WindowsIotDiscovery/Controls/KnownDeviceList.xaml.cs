using Microsoft.Toolkit.Uwp.UI.Animations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
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

            BuildControls();
            BuildInteractions();
        }

        public void BuildControls()
        {

        }

        public void BuildInteractions()
        {
            TbRefresh.PointerEntered += (sender, args) =>
            {
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Hand, 0);
                TbRefresh.Scale(1.1f, 1.1f, 48f, 16f, 300).Start();
            };

            TbRefresh.PointerExited += (sender, args) =>
            {
                Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 0);
                TbRefresh.Scale(1.0f, 1.0f, 48f, 16f).Start();
            };
            TbRefresh.Tapped += (s, e) =>
            {
                ShowLoadingAnimation();
                DiscoveryClient?.Discover();
            };
        }

        void ShowLoadingAnimation()
        {
            RpEmpty.IsHitTestVisible = false;
            PrLoading.Fade(0, 0);
            PrLoading.IsActive = true;
            var animation = RpEmpty.Blur(3, 1000).Then().Blur(0).SetDelay(2500);
            var animation2 = PrLoading.Fade(1).Then().Fade(0).SetDelay(2000);
            animation2.Completed += (s, e) => 
            {
                PrLoading.IsActive = false;
                RpEmpty.IsHitTestVisible = true;
            };
            animation.Start();
            animation2.Start();
        }
    }
}

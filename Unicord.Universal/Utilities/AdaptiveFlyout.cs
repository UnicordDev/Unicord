using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace Unicord.Universal.Utilities
{
    public abstract class AdaptiveFlyout : UserControl
    {
        internal FlyoutBase HostFlyout { get; set; }

        public AdaptiveFlyout(object param)
        {
            DataContext = param;
        }

        protected void CloseHostFlyout()
        {
            HostFlyout?.Hide();
        }
    }

    public static class AdaptiveFlyoutUtilities
    {
        public static void ShowAdaptiveFlyout<TFlyout>(object parameter, FrameworkElement showAt) where TFlyout : AdaptiveFlyout
        {
            var flyout = (AdaptiveFlyout)Activator.CreateInstance(typeof(TFlyout), new[] { parameter });
            //if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile")
            //{
            //    var page = Window.Current.Content.FindChild<MainPage>();
            //}
            //else
            //{
                var flyoutContainer = new Flyout() { Content = flyout };
                flyout.HostFlyout = flyoutContainer;
                flyoutContainer.ShowAt(showAt);
            //}
        }

        public static void ShowAdaptiveFlyout<TFlyout>(object parameter, FrameworkElement showAt, FlyoutPlacementMode placement) where TFlyout : AdaptiveFlyout
        {
            var flyout = (AdaptiveFlyout)Activator.CreateInstance(typeof(TFlyout), new[] { parameter });
            var flyoutContainer = new Flyout() { Content = flyout, Placement = placement };
            flyout.HostFlyout = flyoutContainer;
            flyoutContainer.ShowAt(showAt);
        }
    }
}

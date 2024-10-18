using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Utilities
{
    public abstract class AdaptiveFlyout : UserControl
    {
        public AdaptiveFlyout(object param)
        {
            DataContext = param;
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
                flyoutContainer.ShowAt(showAt);
            //}
        }
    }
}

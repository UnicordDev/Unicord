using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Unicord.Universal.Models.Channels;
using Unicord.Universal.Pages;
using Unicord.Universal.Services;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System.Profile;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Unicord.Universal.Controls.Channels
{
    public sealed partial class ChannelPageHeaderControl : UserControl
    {
        public ChannelPageViewModelBase ViewModel
        {
            get { return (ChannelPageViewModelBase)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register("ViewModel", typeof(ChannelPageViewModelBase), typeof(ChannelPageHeaderControl), new PropertyMetadata(null));

        public ChannelPageHeaderControl()
        {
            this.InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var windowHandle = WindowingService.Current.GetHandle(this);
            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Phone")
                WindowingService.Current.HandleTitleBarForControl(TopGrid);

            if (!WindowingService.Current.IsMainWindow(windowHandle))
            {
                IconGrid.Visibility = Visibility.Visible;
                WindowingService.Current.HandleTitleBarForWindowControls(TopGrid, TitleBarDrag, TitleBarControls, MainControls);
            }

            ShowSidebarButtonContainer.Visibility 
                = this.FindParent<DiscordPage>() == null ? Visibility.Collapsed : Visibility.Visible;
        }

        private void ShowSidebarButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}

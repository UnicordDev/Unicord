using Unicord.Universal.Models.Channels;
using Unicord.Universal.Pages.Overlay;
using Unicord.Universal.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Controls.Flyouts
{
    public sealed partial class DirectMessageContextFlyout : MenuFlyout
    {
        public DirectMessageContextFlyout()
        {
            InitializeComponent();
        }

        private async void ProfileItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is ChannelViewModel channel && channel.Recipient != null)
            {
                await OverlayService.GetForCurrentView()
                    .ShowOverlayAsync<UserInfoOverlayPage>(channel.Recipient);
            }
        }
    }
}

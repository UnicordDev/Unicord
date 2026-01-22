using Unicord.Universal.Models.User;
using Unicord.Universal.Pages.Overlay;
using Unicord.Universal.Services;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Controls.Flyouts
{
    public sealed partial class UserListContextFlyout : MenuFlyout
    {
        public UserListContextFlyout()
        {
            InitializeComponent();
        }

        private async void ProfileItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is UserViewModel user)
            {
                await OverlayService.GetForCurrentView()
                    .ShowOverlayAsync<UserInfoOverlayPage>(user);
            }
        }
    }
}

using System;
using Unicord.Universal.Models.User;
using Unicord.Universal.Services;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Unicord.Universal.Pages.Overlay
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class UserInfoOverlayPage : Page, IOverlay
    {
        public UserInfoOverlayPage()
        {
            this.InitializeComponent();
        }

        public Size PreferredSize =>
            new Size(550, 400);

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is UserViewModel user)
            {
                userInfoOverlay.User = user;
                return;
            }

            if (e.Parameter is System.ValueTuple<UserViewModel, string> tuple)
            {
                userInfoOverlay.User = tuple.Item1;
                userInfoOverlay.NavigateToSection(tuple.Item2);
            }
        }
    }
}

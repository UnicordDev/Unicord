﻿using Unicord.Universal.Models.User;
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

            userInfoOverlay.User = (UserViewModel)e.Parameter;
        }
    }
}

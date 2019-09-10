using DSharpPlus.Entities;
using Unicord.Universal.Utilities;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Unicord.Universal.Controls.Flyouts
{
    public sealed partial class UserFlyout : AdaptiveFlyout
    {
        public UserFlyout(object param) : base(param)
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Window.Current.Content.FindChild<MainPage>()
                .ShowUserOverlay(DataContext as DiscordUser, true);
        }

        // i dislike this
        private void IconLabelButton_Tapped(object sender, TappedRoutedEventArgs e)
        {

        }
    }
}

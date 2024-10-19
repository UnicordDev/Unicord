using DSharpPlus.Entities;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace Unicord.Universal.Pages.Subpages
{
    public sealed partial class AgeGatePage : Page
    {
        private DiscordChannel _channel;

        public AgeGatePage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is DiscordChannel c)
            {
                _channel = c;
            }
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            App.RoamingSettings.Save($"NSFW_{_channel.Id}", true);

            if (notAgain.IsChecked == true)
            {
                App.RoamingSettings.Save("NSFW_All", true);
            }

            Frame.Navigate(typeof(ChannelPage), _channel, new DrillInNavigationTransitionInfo());
        }
    }
}

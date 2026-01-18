using Unicord.Universal.Models.Channels;
using Unicord.Universal.Services;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Unicord.Universal.Pages.Subpages
{
    public sealed partial class PinsPage : Page
    {
        private ChannelViewModel _channel;

        public PinsPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is ChannelViewModel channel)
            {
                _channel = channel;
                DataContext = new PinsPageViewModel(channel);
            }
        }

        private void CloseButton_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            SplitPaneService.GetForCurrentView().ToggleRightPane<PinsPage>(_channel);
        }
    }
}

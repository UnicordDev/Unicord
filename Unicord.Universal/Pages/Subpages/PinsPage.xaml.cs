using Unicord.Universal.Models.Channels;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Unicord.Universal.Pages.Subpages
{
    public sealed partial class PinsPage : Page
    {
        public PinsPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is ChannelViewModel channel)
            {
                DataContext = new PinsPageViewModel(channel);
            }
        }
    }
}

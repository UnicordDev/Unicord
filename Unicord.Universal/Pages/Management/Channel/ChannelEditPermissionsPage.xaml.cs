using Unicord.Universal.Models;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Unicord.Universal.Pages.Management.Channel
{
    public sealed partial class ChannelEditPermissionsPage : Page
    {
        public ChannelEditPermissionsPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is ChannelPermissionEditViewModel model)
                DataContext = model;
        }
    }
}

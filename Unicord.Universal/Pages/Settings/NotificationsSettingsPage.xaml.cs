using Microsoft.Toolkit.Uwp.Helpers;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Unicord.Universal.Pages.Settings
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class NotificationsSettingsPage : Page
    {
        public string NotificationGlyph { get; } = SystemInformation.Instance.OperatingSystemVersion.Build >= 22000 ? "\uEA8F" : "\uE91C";

        public NotificationsSettingsPage()
        {
            this.InitializeComponent();
        }
    }
}

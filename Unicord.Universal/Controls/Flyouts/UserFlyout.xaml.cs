using DSharpPlus.Entities;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Controls.Flyouts
{
    public sealed partial class UserFlyout : Flyout
    {
        public object DataContext
        {
            get => (Content as FrameworkElement).DataContext;
            set => (Content as FrameworkElement).DataContext = value;
        }

        public UserFlyout()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Hide();
            Window.Current.Content.FindChild<MainPage>()
                .ShowUserOverlay(DataContext as DiscordUser, true);
        }
    }
}

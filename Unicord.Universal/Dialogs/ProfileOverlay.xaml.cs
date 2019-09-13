using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Unicord.Universal.UICommands;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Shapes;

namespace Unicord.Universal.Dialogs
{
    public sealed partial class ProfileOverlay : UserControl
    {
        DiscordUser _user;
        DiscordMember _member;
        MessageUserCommand _messageUserCommand = new MessageUserCommand();

        bool _isMember => _member != null;

        public DiscordUser User { get => GetValue(UserProperty) as DiscordUser; set => SetValue(UserProperty, value); }
        public Ellipse AnimatedEllipse => ellipse;

        public static readonly DependencyProperty UserProperty =
            DependencyProperty.Register("User", typeof(DiscordUser), typeof(ProfileOverlay), new PropertyMetadata(null, User_Changed));

        private static async void User_Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var overlay = (ProfileOverlay)d;
            overlay._user = e.NewValue as DiscordUser;
            overlay._member = e.NewValue as DiscordMember;
            overlay.Bindings.Update();

            await overlay.LoadedAsync();
        }

        public ProfileOverlay()
        {
            InitializeComponent();
        }

        private async Task LoadedAsync()
        {
            var mutualGuilds =
                await Task.Run(() => App.Discord.Guilds.Values.Where(g => g.Members.ContainsKey(_user.Id)).OrderBy(g => g.Name));

            mutualServers.ItemsSource = mutualGuilds;
        }

        private void DropShadowPanel_PreviewKeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Escape)
            {
                this.FindParent<MainPage>().HideOverlay();
            }
        }
    }
}

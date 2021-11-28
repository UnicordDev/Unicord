using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Unicord.Universal.Commands;
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
        private DiscordUser _user;
        private DiscordMember _member;

        private bool _isMember => _member != null;

        public DiscordUser User { get => GetValue(UserProperty) as DiscordUser; set => SetValue(UserProperty, value); }


        public static readonly DependencyProperty UserProperty =
            DependencyProperty.Register("User", typeof(DiscordUser), typeof(ProfileOverlay), new PropertyMetadata(null, OnUserChanged));

        private static void OnUserChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var overlay = (ProfileOverlay)d;
            overlay._user = e.NewValue as DiscordUser;
            overlay._member = e.NewValue as DiscordMember;
            overlay.mutualServers.ItemsSource = App.Discord.Guilds.Values.Where(g => g.Members.ContainsKey(overlay._user.Id));
            overlay.Bindings.Update();
        }

        public ProfileOverlay()
        {
            InitializeComponent();
        }

        private void DropShadowPanel_PreviewKeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Escape)
            {
                this.FindParent<MainPage>().HideUserOverlay();
            }
        }
    }
}

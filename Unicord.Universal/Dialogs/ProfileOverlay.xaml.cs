using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Unicord.Universal.Commands;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Unicord.Universal.Dialogs
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
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
                await Task.Run(() => App.Discord.Guilds.Values.Where(g => g.Members.Any(u => u.Id == _user.Id)).OrderBy(g => g.Name));

            mutualServers.ItemsSource = mutualGuilds;

            var imageAnimation =
                ConnectedAnimationService.GetForCurrentView().GetAnimation("image");
            if (imageAnimation != null)
            {
                imageAnimation.TryStart(ellipse);
            }
        }
    }
}

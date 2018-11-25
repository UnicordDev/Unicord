using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Unicord.Universal.Dialogs
{
    public sealed partial class GuildDialog : UserControl
    {
        public DiscordGuild Guild
        {
            get { return (DiscordGuild)GetValue(GuildProperty); }
            set { SetValue(GuildProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Guild.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GuildProperty =
            DependencyProperty.Register("Guild", typeof(DiscordGuild), typeof(GuildDialog), new PropertyMetadata(null, OnGuildChanged));

        private static void OnGuildChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var guild = e.NewValue as DiscordGuild;
            var guildDialog = d as GuildDialog;
            guildDialog.DataContext = guild;

            if (guild.Channels.Any(c => c.IsCategory))
            {
                // Use new discord channel category behaviour
                guildDialog.cvs.IsSourceGrouped = true;
                guildDialog.cvs.Source = guild.Channels
                    .Where(c => !c.IsCategory)
                    .OrderBy(c => c.Position)
                    .OrderBy(c => c.Type)
                    .GroupBy(g => g.Parent)
                    .OrderBy(c => c.Key?.Position);
            }
            else
            {
                // Use old discord non-category behaviour
                guildDialog.cvs.IsSourceGrouped = false;
                guildDialog.cvs.Source = guild.Channels
                    .OrderBy(c => c.Position)
                    .OrderBy(c => c.Type);
            }
        }

        public GuildDialog()
        {
            InitializeComponent();
            regionsBox.ItemsSource = App.Discord.VoiceRegions.Values;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.FindParent<ContentControl>().Visibility = Visibility.Collapsed;
        }
    }
}

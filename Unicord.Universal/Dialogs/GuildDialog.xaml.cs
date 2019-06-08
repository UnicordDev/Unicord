using System.Linq;
using DSharpPlus.Entities;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Dialogs
{
    public sealed partial class GuildDialog : UserControl
    {
        public DiscordGuild Guild
        {
            get => (DiscordGuild)GetValue(GuildProperty);
            set => SetValue(GuildProperty, value);
        }

        public static readonly DependencyProperty GuildProperty =
            DependencyProperty.Register("Guild", typeof(DiscordGuild), typeof(GuildDialog), new PropertyMetadata(null, OnGuildChanged));

        private static void OnGuildChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var guild = e.NewValue as DiscordGuild;
            var guildDialog = d as GuildDialog;
            guildDialog.DataContext = guild;

            if (guild.Channels.Values.Any(c => c.IsCategory))
            {
                // Use new discord channel category behaviour
                guildDialog.cvs.IsSourceGrouped = true;
                guildDialog.cvs.Source = guild.Channels.Values
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
                guildDialog.cvs.Source = guild.Channels.Values
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

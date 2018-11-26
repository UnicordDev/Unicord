using DSharpPlus;
using DSharpPlus.Entities;
using NeoSmart.Unicode;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Unicord.Universal.Controls
{
    public sealed partial class EmotePicker : UserControl
    {
        public event EventHandler<DiscordEmoji> EmojiPicked;

        public DiscordChannel Channel
        {
            get { return (DiscordChannel)GetValue(ChannelProperty); }
            set { SetValue(ChannelProperty, value); }
        }

        public static readonly DependencyProperty ChannelProperty =
            DependencyProperty.Register("Channel", typeof(DiscordChannel), typeof(EmotePicker), new PropertyMetadata(null));

        public EmotePicker()
        {
            InitializeComponent();
        }

        public async Task Load()
        {
            try
            {
                IEnumerable<DiscordEmoji> enumerable = null;

                if (Channel.IsPrivate || Channel.PermissionsFor(Channel.Guild.CurrentMember).HasFlag(Permissions.UseExternalEmojis) && App.Discord.CurrentUser.HasNitro)
                {
                    enumerable = App.Discord.Guilds.Values
                        .SelectMany(g => g.Emojis);
                }
                else
                {
                    enumerable = Channel.Guild.Emojis;
                }

                enumerable = enumerable.OrderBy(g => g.Name);

                if (!string.IsNullOrWhiteSpace(searchBox.Text))
                {
                    source.IsSourceGrouped = false;

                    var text = searchBox.Text.ToLowerInvariant();
                    source.Source =
                        enumerable.Where(s => s.Name.ToLowerInvariant().Contains(text));
                }
                else
                {
                    source.IsSourceGrouped = true;

                    source.Source =
                        await Task.Run(() => enumerable.GroupBy(em => em.Discord.Guilds.Values.First(g => g.Emojis.Contains(em)))
                        .OrderBy(g => App.Discord.UserSettings.GuildPositions.IndexOf(g.Key.Id))
                        .ToList());
                }
            }
            catch { }
        }

        public void Unload()
        {
            source.Source = null;
        }

        private void GridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Any())
            {
                EmojiPicked?.Invoke(this, e.AddedItems.FirstOrDefault() as DiscordEmoji);
                (sender as GridView).SelectedItem = null;
            }
        }

        private async void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(searchBox.Text) || searchBox.Text.Length > 2)
            {
                await Load();
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            InputPane.GetForCurrentView()?.TryHide();
            await Load();
        }        
    }
}

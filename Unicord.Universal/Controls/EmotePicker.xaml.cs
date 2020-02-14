using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using Unicord.Universal.Misc;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Controls
{
    public sealed partial class EmotePicker : UserControl
    {
        public event EventHandler<DiscordEmoji> EmojiPicked;
        public static Emoji[] Emoji { get; internal set; }

        public DiscordChannel Channel
        {
            get => (DiscordChannel)GetValue(ChannelProperty);
            set => SetValue(ChannelProperty, value);
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
                if (Emoji == null)
                {
                    var emojiFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/emoji.json"));
                    var emojiList = await FileIO.ReadTextAsync(emojiFile);
                    Emoji = await Task.Run(() => JsonConvert.DeserializeObject<Emoji[]>(emojiList));
                }

                IEnumerable<DiscordEmoji> enumerable = null;

                if (Channel.IsPrivate || Channel.PermissionsFor(Channel.Guild.CurrentMember).HasFlag(Permissions.UseExternalEmojis) && App.Discord.CurrentUser.HasNitro())
                {
                    enumerable = App.Discord.Guilds.Values
                        .SelectMany(g => g.Emojis.Values);
                }
                else
                {
                    enumerable = Channel.Guild.Emojis.Values;
                }

                enumerable = enumerable.OrderBy(g => g.Name);                

                var text = searchBox.Text.ToLowerInvariant();
                var cult = CultureInfo.InvariantCulture.CompareInfo;
                var n = !string.IsNullOrWhiteSpace(text);

                source.IsSourceGrouped = true;
                source.Source = await Task.Run(() =>
                {
                    var emojiEnum = Emoji
                            .Where(e => n ? cult.IndexOf(e.Name, text, CompareOptions.IgnoreCase) >= 0 : true)
                            .GroupBy(e => e.Category)
                            .Select(g => new EmojiGroup(g.Key, g))
                            .ToList();

                    var list = enumerable
                        .Where(e => n ? cult.IndexOf(e.GetDiscordName(), text, CompareOptions.IgnoreCase) >= 0 : true)
                        .GroupBy(e => App.Discord.Guilds.Values.FirstOrDefault(g => g.Emojis.ContainsKey(e.Id)))
                        .OrderBy(g => App.Discord.UserSettings.GuildPositions.IndexOf(g.Key.Id))
                        .Select(g => new EmojiGroup(g.Key, g))
                        .ToList();

                    list.AddRange(emojiEnum);

                    return list;
                });
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

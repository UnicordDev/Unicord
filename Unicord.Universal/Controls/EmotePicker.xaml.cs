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
                source.IsSourceGrouped = true;
                source.Source = await Tools.GetGroupedEmojiAsync(searchBox.Text.ToLowerInvariant(), Channel);
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

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            await Load();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            Unload();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using Unicord.Universal.Misc;
using Unicord.Universal.Models.Channels;
using Unicord.Universal.Models.Emoji;
using Unicord.Universal.Utilities;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Unicord.Universal.Controls
{
    public sealed partial class EmotePicker : UserControl
    {
        private ulong _prevChannelId;

        public CollectionViewSource Source { get; } = new CollectionViewSource() { IsSourceGrouped = true };
        public event EventHandler<EmojiViewModel> EmojiPicked;

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

        public void Load()
        {
            try
            {
                Source.Source = EmojiUtilities.GetEmoji(new ChannelViewModel(Channel.Id, true), searchBox.Text);
            }
            catch { }
        }

        public void Unload()
        {
            // Source.Source = null;
        }

        private void EmojiView_ItemClick(object sender, ItemClickEventArgs e)
        {
            EmojiPicked?.Invoke(this, (EmojiViewModel)e.ClickedItem);
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(searchBox.Text) || searchBox.Text.Length > 2)
            {
                Load();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            InputPane.GetForCurrentView()?.TryHide();

            Load();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Load();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            Unload();
        }
    }
}

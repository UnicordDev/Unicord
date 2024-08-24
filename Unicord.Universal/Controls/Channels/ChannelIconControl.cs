using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using NeoSmart.Unicode;
using System.Diagnostics;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace Unicord.Universal.Controls.Channels
{
    public sealed class ChannelIconControl : Control
    {
        public DiscordEmoji Emoji
        {
            get { return (DiscordEmoji)GetValue(EmojiProperty); }
            set { SetValue(EmojiProperty, value); }
        }

        public static readonly DependencyProperty EmojiProperty =
            DependencyProperty.Register("Emoji", typeof(DiscordEmoji), typeof(ChannelIconControl), new PropertyMetadata(null));

        public DiscordChannel Channel
        {
            get { return (DiscordChannel)GetValue(ChannelProperty); }
            set { SetValue(ChannelProperty, value); }
        }

        public static readonly DependencyProperty ChannelProperty =
            DependencyProperty.Register("Channel", typeof(DiscordChannel), typeof(ChannelIconControl), new PropertyMetadata(null));

        public ChannelIconControl()
        {
            this.DefaultStyleKey = typeof(ChannelIconControl);
        }
    }
}

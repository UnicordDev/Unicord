using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using DSharpPlus.Entities;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Unicord.Universal.Controls.Emoji
{
    public sealed class EmojiControl : Control
    {
        public DiscordEmoji Emoji
        {
            get => (DiscordEmoji)GetValue(EmojiProperty);
            set => SetValue(EmojiProperty, value);
        }

        public static readonly DependencyProperty EmojiProperty =
            DependencyProperty.Register("Emoji", typeof(DiscordEmoji), typeof(EmojiControl), new PropertyMetadata(null));

        public double Size
        {
            get => (double)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register("Size", typeof(double), typeof(EmojiControl), new PropertyMetadata(32));

        public EmojiControl()
        {
            this.DefaultStyleKey = typeof(EmojiControl);
        }
    }
}

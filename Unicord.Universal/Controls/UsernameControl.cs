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

namespace Unicord.Universal.Controls
{
    public sealed class UsernameControl : Control
    {
        public DiscordUser User
        {
            get => (DiscordUser)GetValue(UserProperty);
            set => SetValue(UserProperty, value);
        }

        public static readonly DependencyProperty UserProperty =
            DependencyProperty.Register("User", typeof(DiscordUser), typeof(UsernameControl), new PropertyMetadata(null));

        public double IconSize
        {
            get => (double)GetValue(IconSizeProperty);
            set => SetValue(IconSizeProperty, value);
        }

        public static readonly DependencyProperty IconSizeProperty =
            DependencyProperty.Register("IconSize", typeof(double), typeof(UsernameControl), new PropertyMetadata(16));

        public UsernameControl()
        {
            this.DefaultStyleKey = typeof(UsernameControl);
        }
    }
}

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

namespace Unicord.Universal.Controls.Presence
{
    public sealed class StatusIndicator : Control
    {
        public DiscordPresence Presence
        {
            get { return (DiscordPresence)GetValue(PresenceProperty); }
            set { SetValue(PresenceProperty, value); }
        }

        public static readonly DependencyProperty PresenceProperty =
            DependencyProperty.Register("Presence", typeof(DiscordPresence), typeof(StatusIndicator), new PropertyMetadata(null));

        public StatusIndicator()
        {
            this.DefaultStyleKey = typeof(StatusIndicator);
        }
    }
}

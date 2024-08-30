using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using DSharpPlus.Entities;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Unicord.Universal.Controls.Presence
{
    public class CondencedPresence
    {
        public DiscordEmoji Emoji { get; set; }
        public string StatusText { get; set; }
        public string StatusName { get; set; }

        public bool Empty =>
            Emoji == null && string.IsNullOrWhiteSpace(StatusText) && string.IsNullOrWhiteSpace(StatusName);
    }

    public sealed partial class InlinePresenceControl : UserControl
    {
        private ResourceLoader _strings;

        public DiscordPresence Presence
        {
            get => (DiscordPresence)GetValue(PresenceProperty);
            set => SetValue(PresenceProperty, value);
        }

        public static readonly DependencyProperty PresenceProperty =
            DependencyProperty.Register("Presence", typeof(DiscordPresence), typeof(InlinePresenceControl), new PropertyMetadata(null, OnPresenceChanged));

        public TextWrapping TextWrapping
        {
            get => (TextWrapping)GetValue(TextWrappingProperty);
            set => SetValue(TextWrappingProperty, value);
        }

        public static readonly DependencyProperty TextWrappingProperty =
            DependencyProperty.Register("TextWrapping", typeof(TextWrapping), typeof(InlinePresenceControl), new PropertyMetadata(TextWrapping.NoWrap));

        public CondencedPresence CondencedPresence
        {
            get => (CondencedPresence)GetValue(CondencedPresenceProperty);
            set => SetValue(CondencedPresenceProperty, value);
        }

        public static readonly DependencyProperty CondencedPresenceProperty =
            DependencyProperty.Register("CondencedPresence", typeof(CondencedPresence), typeof(InlinePresenceControl), new PropertyMetadata(null));

        public InlinePresenceControl()
        {
            this.InitializeComponent();
            _strings = ResourceLoader.GetForViewIndependentUse("Converters");
        }

        private static void OnPresenceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is InlinePresenceControl control))
                return;

            if (!(e.NewValue is DiscordPresence newPresence) || newPresence.Activities == null)
            {
                control.CondencedPresence = null;
                return;
            }

            var strings = control._strings;
            var condensedPresence = new CondencedPresence();
            foreach (var presence in newPresence.Activities.Where(p => p != null).OrderBy(p => p.ActivityType))
            {
                switch (presence.ActivityType)
                {
                    case ActivityType.Playing:
                        condensedPresence.StatusText = strings.GetString("PlayingStatus");
                        condensedPresence.StatusName = presence.Name;
                        break;
                    case ActivityType.Streaming:
                        condensedPresence.StatusText = strings.GetString("StreamingStatus");
                        condensedPresence.StatusName = presence.RichPresence?.Details ?? presence.Name;
                        break;
                    case ActivityType.ListeningTo:
                        condensedPresence.StatusText = strings.GetString("ListeningStatus");
                        condensedPresence.StatusName = presence.Name;
                        break;
                    case ActivityType.Watching:
                        condensedPresence.StatusText = strings.GetString("WatchingStatus");
                        condensedPresence.StatusName = presence.Name;
                        break;
                    case ActivityType.Custom:
                        {
                            var custom = presence.CustomStatus;
                            if (!string.IsNullOrWhiteSpace(custom.Name))
                            {
                                condensedPresence.StatusText = custom.Name;
                                condensedPresence.StatusName = null;
                            }

                            condensedPresence.Emoji = custom.Emoji;
                            break;
                        }
                    default:
                        break;
                }
            }

            control.CondencedPresence = condensedPresence;
        }
    }
}

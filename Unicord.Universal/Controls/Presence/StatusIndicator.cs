using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Unicord.Universal.Models.User;
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
        public PresenceViewModel Presence
        {
            get { return (PresenceViewModel)GetValue(PresenceProperty); }
            set { SetValue(PresenceProperty, value); }
        }

        public static readonly DependencyProperty PresenceProperty =
            DependencyProperty.Register("Presence", typeof(PresenceViewModel), typeof(StatusIndicator), new PropertyMetadata(null));

        public StatusIndicator()
        {
            this.DefaultStyleKey = typeof(StatusIndicator);
        }
    }
}

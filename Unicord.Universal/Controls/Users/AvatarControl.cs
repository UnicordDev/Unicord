using System;
using Unicord.Universal.Models.User;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace Unicord.Universal.Controls.Users
{
    public sealed class AvatarControl : Control
    {
        public Uri Source
        {
            get { return (Uri)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(Uri), typeof(AvatarControl), new PropertyMetadata(null));

        public PresenceViewModel Presence
        {
            get { return (PresenceViewModel)GetValue(PresenceProperty); }
            set { SetValue(PresenceProperty, value); }
        }

        public static readonly DependencyProperty PresenceProperty =
            DependencyProperty.Register("Presence", typeof(PresenceViewModel), typeof(AvatarControl), new PropertyMetadata(null));

        public AvatarControl()
        {
            this.DefaultStyleKey = typeof(AvatarControl);
        }
    }
}

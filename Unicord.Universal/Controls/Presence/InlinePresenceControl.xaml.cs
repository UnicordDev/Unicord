using Unicord.Universal.Models.User;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Controls.Presence
{
    public sealed partial class InlinePresenceControl : UserControl
    {
        public PresenceViewModel Presence
        {
            get => (PresenceViewModel)GetValue(PresenceProperty);
            set => SetValue(PresenceProperty, value);
        }

        public static readonly DependencyProperty PresenceProperty =
            DependencyProperty.Register("Presence", typeof(PresenceViewModel), typeof(InlinePresenceControl), new PropertyMetadata(null, OnPresenceUpdated));

        private static void OnPresenceUpdated(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not InlinePresenceControl control) return;
            control.Bindings.Update();
        }

        public TextWrapping TextWrapping
        {
            get => (TextWrapping)GetValue(TextWrappingProperty);
            set => SetValue(TextWrappingProperty, value);
        }

        public static readonly DependencyProperty TextWrappingProperty =
            DependencyProperty.Register("TextWrapping", typeof(TextWrapping), typeof(InlinePresenceControl), new PropertyMetadata(TextWrapping.NoWrap));

        public InlinePresenceControl()
        {
            this.InitializeComponent();
        }

        public bool Not(bool b)
            => !b;
    }
}

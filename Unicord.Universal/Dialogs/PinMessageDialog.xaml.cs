using System;
using DSharpPlus.Entities;
using Unicord.Universal.Controls;
using Unicord.Universal.Controls.Messages;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Unicord.Universal.Dialogs
{
    public sealed partial class PinMessageDialog : ContentDialog
    {
        public DiscordMessage Message
        {
            get => (DiscordMessage)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(DiscordMessage), typeof(PinMessageDialog), new PropertyMetadata(null, OnMessageSet));

        private static void OnMessageSet(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as ContentDialog).DataContext = e.NewValue;
            (d as ContentDialog).Title = (e.NewValue as DiscordMessage).Pinned ? "Unpin this message?" : "Pin this message?";
        }

        public PinMessageDialog()
        {
            InitializeComponent();
        }
    }
}

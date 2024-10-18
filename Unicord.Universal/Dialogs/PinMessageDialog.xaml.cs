﻿using DSharpPlus.Entities;
using Unicord.Universal.Models.Messages;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

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
            // todo: why
            (d as PinMessageDialog).MessageControl.MessageViewModel = new MessageViewModel(e.NewValue as DiscordMessage);
            (d as ContentDialog).Title = (e.NewValue as DiscordMessage).Pinned ? "Unpin this message?" : "Pin this message?";
        }

        public PinMessageDialog()
        {
            InitializeComponent();
        }
    }
}

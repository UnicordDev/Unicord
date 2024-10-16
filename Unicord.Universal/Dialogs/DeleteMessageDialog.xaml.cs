using DSharpPlus.Entities;
using Unicord.Universal.Models.Messages;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Dialogs
{
    public sealed partial class DeleteMessageDialog : ContentDialog
    {
        public DiscordMessage Message
        {
            get => (DiscordMessage)GetValue(MessageProperty);
            set => SetValue(MessageProperty, value);
        }

        public static readonly DependencyProperty MessageProperty =
            DependencyProperty.Register("Message", typeof(DiscordMessage), typeof(DeleteMessageDialog), new PropertyMetadata(null, OnMessageSet));

        private static void OnMessageSet(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // todo: why
            (d as DeleteMessageDialog).MessageControl.MessageViewModel = new MessageViewModel(e.NewValue as DiscordMessage);
        }

        public DeleteMessageDialog()
        {
            InitializeComponent();
        }
    }
}

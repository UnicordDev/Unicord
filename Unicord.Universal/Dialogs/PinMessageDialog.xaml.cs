using DSharpPlus.Entities;
using Unicord.Universal.Controls;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Unicord.Universal.Dialogs
{
    public sealed partial class PinMessageDialog : ContentDialog
    {
        public PinMessageDialog(DiscordMessage message)
        {
            InitializeComponent();
            Title = message.Pinned ? "Unpin this message?" : "Pin this message?";
            Content = new MessageViewer() { Message = message, IsEnabled = false, Background = new SolidColorBrush(Colors.Transparent) };
        }
    }
}

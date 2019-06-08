using DSharpPlus.Entities;
using Unicord.Universal.Controls;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Unicord.Universal.Dialogs
{
    public sealed partial class DeleteMessageDialog : ContentDialog
    {
        public DeleteMessageDialog(DiscordMessage message)
        {
            InitializeComponent();
            Content = new MessageViewer() { Message = message, IsEnabled = false, Background = new SolidColorBrush(Colors.Transparent) };
        }
    }
}

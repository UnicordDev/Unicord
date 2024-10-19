using DSharpPlus;
using Unicord.Universal.Models;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Pages.Management.Channel
{
    public sealed partial class ChannelEditOverviewPage : Page
    {
        public ChannelEditOverviewPage()
        {
            this.InitializeComponent();
        }

        private void TextBox_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
        {
            var model = (DataContext as ChannelEditViewModel);
            if (model.Channel.Type == ChannelType.Text)
            {
                sender.Text = sender.Text.Replace(' ', '-').ToLowerInvariant();
                sender.Select(sender.Text.Length, 0);
            }
        }
    }
}

using Unicord.Universal.Controls.Messages;
using Unicord.Universal.Extensions;
using Unicord.Universal.Pages;
using Windows.UI.Xaml;
using MUXC = Microsoft.UI.Xaml.Controls;

namespace Unicord.Universal.Controls.Flyouts
{
    public sealed partial class MessageContextFlyout : MUXC.CommandBarFlyout
    {
        public MessageContextFlyout()
        {
            this.InitializeComponent();
        }

        // todo: is there a less shit way of doing this?
        private void AddReactionButton_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            var control = this.Target.FindParent<MessageControl>();
            var page = this.Target.FindParent<ChannelPage>();

            page.ShowReactionPicker(control.MessageViewModel);
        }

        // HACK
        private void HideOnClick(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }
    }
}

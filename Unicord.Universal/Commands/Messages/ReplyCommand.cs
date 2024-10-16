using DSharpPlus;
using Microsoft.AppCenter.Analytics;
using Unicord.Universal.Extensions;
using Unicord.Universal.Models;
using Unicord.Universal.Models.Messages;
using Unicord.Universal.Pages;
using Windows.UI.Xaml;

namespace Unicord.Universal.Commands
{
    public class ReplyCommand : DiscordCommand<MessageViewModel>
    {
        public ReplyCommand(MessageViewModel viewModel) : base(viewModel)
        {
        }

        public override bool CanExecute(object parameter)
        {
            return viewModel.Channel is ChannelPageViewModel &&
                (viewModel.Type is MessageType.Default or MessageType.Reply or MessageType.ApplicationCommand);
        }

        public override void Execute(object parameter)
        {
            Analytics.TrackEvent("ReplyCommand_Invoked");

            if (viewModel.Channel is not ChannelPageViewModel pageViewModel)
                return;

            pageViewModel.ReplyTo = viewModel;

            // TODO: sorta messy?
            var channelPage = Window.Current.Content.FindChild<ChannelPage>();
            if (channelPage == null)
                return;
            channelPage.FocusTextBox();
        }
    }
}

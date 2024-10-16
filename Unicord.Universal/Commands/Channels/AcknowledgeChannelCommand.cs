using Unicord.Universal.Models.Channels;

namespace Unicord.Universal.Commands.Channels
{
    internal class AcknowledgeChannelCommand : DiscordCommand<ChannelViewModel>
    {
        public AcknowledgeChannelCommand(ChannelViewModel viewModel) : base(viewModel)
        {
        }

        public override bool CanExecute(object parameter)
        {
            return viewModel.Unread && viewModel.Channel.LastMessageId != null;
        }

        public override async void Execute(object parameter)
        {
            if (!viewModel.Unread || viewModel.Channel.LastMessageId == null) return;

            await viewModel.Channel.AcknowledgeAsync(viewModel.Channel.LastMessageId.Value);
        }
    }
}

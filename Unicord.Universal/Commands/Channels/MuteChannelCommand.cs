using Unicord.Universal.Models.Channels;

namespace Unicord.Universal.Commands.Channels
{
    internal class MuteChannelCommand : DiscordCommand<ChannelViewModel>
    {
        public MuteChannelCommand(ChannelViewModel viewModel) : base(viewModel)
        {
        }

        public override bool CanExecute(object parameter)
        {
            return false;
        }

        public override void Execute(object parameter)
        {

        }
    }
}

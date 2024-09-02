using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unicord.Universal.Extensions;
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

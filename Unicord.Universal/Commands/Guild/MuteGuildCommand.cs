using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unicord.Universal.Models.Guild;

namespace Unicord.Universal.Commands.Guild
{
    internal class MuteGuildCommand : DiscordCommand<GuildViewModel>
    {
        public MuteGuildCommand(GuildViewModel viewModel) : base(viewModel)
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
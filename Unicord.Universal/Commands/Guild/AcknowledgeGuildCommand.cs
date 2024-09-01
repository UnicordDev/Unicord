using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unicord.Universal.Models.Guild;

namespace Unicord.Universal.Commands.Guild
{
    internal class AcknowledgeGuildCommand : DiscordCommand<GuildViewModel>
    {
        public AcknowledgeGuildCommand(GuildViewModel viewModel) : base(viewModel)
        {
        }

        public override bool CanExecute(object parameter)
        {
            return viewModel.Unread;
        }

        public override async void Execute(object parameter)
        {
            if (!viewModel.Unread)
                return;

            await viewModel.Guild.AcknowledgeAsync();
        }
    }
}

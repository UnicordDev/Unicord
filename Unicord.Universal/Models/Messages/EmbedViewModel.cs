using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace Unicord.Universal.Models.Messages
{
    public class EmbedViewModel : ViewModelBase
    {
        private DiscordEmbed embed;
        private MessageViewModel messageViewModel;

        public EmbedViewModel(DiscordEmbed embed, ViewModelBase parent)
            : base(parent)
        {
            this.embed = embed;
            this.messageViewModel = parent as MessageViewModel;
        }
    }
}

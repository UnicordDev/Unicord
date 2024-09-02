using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace Unicord.Universal.Models.Messages.Components
{
    public class UnknownComponentViewModel : ComponentViewModelBase
    {
        public UnknownComponentViewModel(DiscordComponent component, MessageViewModel messageViewModel) 
            : base(component, messageViewModel)
        {
        }
    }
}

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

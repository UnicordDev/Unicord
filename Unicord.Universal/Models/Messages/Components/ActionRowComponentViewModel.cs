using System;
using System.Collections.ObjectModel;
using System.Linq;
using DSharpPlus.Entities;

namespace Unicord.Universal.Models.Messages.Components
{
    public class ActionRowComponentViewModel : ComponentViewModelBase
    {
        public ActionRowComponentViewModel(
            DiscordActionRowComponent component, 
            Func<DiscordComponent, ComponentViewModelBase> componentViewModelFactory,
            MessageViewModel messageViewModel) 
            : base(component, messageViewModel)
        {
            Components = new ObservableCollection<ComponentViewModelBase>(component.Components.Select(componentViewModelFactory));
        }

        public ObservableCollection<ComponentViewModelBase> Components { get; }
    }
}

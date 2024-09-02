using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

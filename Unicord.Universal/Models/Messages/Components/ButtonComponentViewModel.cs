using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Toolkit.Mvvm.Input;
using Unicord.Universal.Models.Emoji;
using Windows.System;

namespace Unicord.Universal.Models.Messages.Components
{
    public class ButtonComponentViewModel : ComponentViewModelBase
    {
        public ButtonComponentViewModel(DiscordLinkButtonComponent component, MessageViewModel messageViewModel)
            : base(component, messageViewModel)
        {
            Style = ButtonStyle.Link;
            Emoji = new EmojiViewModel(component.Emoji);
            Label = component.Label;
            ShowExternalLinkIcon = true;
            Execute = new AsyncRelayCommand(async () => await Launcher.LaunchUriAsync(new Uri(component.Url)));
        }

        public ButtonComponentViewModel(DiscordButtonComponent component, MessageViewModel messageViewModel)
            : base(component, messageViewModel)
        {
            Style = component.Style;
            Emoji = new EmojiViewModel(component.Emoji);
            Label = component.Label;
            ShowExternalLinkIcon = false;
        }

        public ButtonStyle Style { get; }
        public EmojiViewModel Emoji { get; }
        public string Label { get; }
        public bool HasLabel
            => Label != null;
        public bool ShowExternalLinkIcon { get; }
        public ICommand Execute { get; }
    }
}

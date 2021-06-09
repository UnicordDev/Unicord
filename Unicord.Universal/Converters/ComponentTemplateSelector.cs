using System;
using DSharpPlus;
using DSharpPlus.Entities;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Converters
{
    public class ComponentTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ActionRowTemplate { get; set; }
        public DataTemplate PrimaryButtonTemplate { get; set; }
        public DataTemplate SecondaryButtonTemplate { get; set; }
        public DataTemplate SuccessButtonTemplate { get; set; }
        public DataTemplate DangerButtonTemplate { get; set; }
        public DataTemplate LinkButtonTemplate { get; set; }
        public DataTemplate UnknownTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (item is DiscordComponent component)
            {
                switch (component.Type)
                {
                    case DiscordInteractionType.ActionRow:
                        return ActionRowTemplate;
                    case DiscordInteractionType.Button:
                        switch (component.ButtonStyle.Value)
                        {
                            case DiscordButtonStyle.Primary:
                                return PrimaryButtonTemplate;
                            case DiscordButtonStyle.Secondary:
                                return SecondaryButtonTemplate;
                            case DiscordButtonStyle.Success:
                                return SuccessButtonTemplate;
                            case DiscordButtonStyle.Danger:
                                return DangerButtonTemplate;
                            case DiscordButtonStyle.Link:
                                return LinkButtonTemplate;
                            default:
                                break;
                        }
                        break;
                    default:
                        return UnknownTemplate;
                }
            }

            throw new InvalidOperationException();
        }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            return SelectTemplateCore(item);
        }
    }
}

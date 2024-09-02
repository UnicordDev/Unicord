using System;
using DSharpPlus;
using DSharpPlus.Entities;
using Unicord.Universal.Models.Messages.Components;
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
            switch (item)
            {
                case ActionRowComponentViewModel _: 
                    return ActionRowTemplate;
                case ButtonComponentViewModel button:
                    switch (button.Style)
                    {
                        case ButtonStyle.Primary:
                            return PrimaryButtonTemplate;
                        case ButtonStyle.Secondary:
                            return SecondaryButtonTemplate;
                        case ButtonStyle.Success:
                            return SuccessButtonTemplate;
                        case ButtonStyle.Danger:
                            return DangerButtonTemplate;
                        case ButtonStyle.Link:
                            return LinkButtonTemplate;
                        default:
                            break;
                    }
                    break;
            }
            return UnknownTemplate;
        }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            return SelectTemplateCore(item);
        }
    }
}

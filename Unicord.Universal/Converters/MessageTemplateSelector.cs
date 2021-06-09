using DSharpPlus;
using DSharpPlus.Entities;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Converters
{
    public class MessageTemplateSelector : DataTemplateSelector
    {
        public DataTemplate MessageTemplate { get; set; }
        public DataTemplate SystemMessageTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {
            if (item is DiscordMessage message)
            {
                if (message.MessageType != MessageType.Default && message.MessageType != MessageType.Reply)
                    return SystemMessageTemplate;
            }

            return MessageTemplate;
        }
    }
}

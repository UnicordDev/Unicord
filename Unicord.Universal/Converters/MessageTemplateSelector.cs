using Unicord.Universal.Models.Messages;
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
            if (item is MessageViewModel message)
            {
                if (message.IsSystemMessage)
                    return SystemMessageTemplate;
            }

            return MessageTemplate;
        }
    }
}

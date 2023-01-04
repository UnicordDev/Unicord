using DSharpPlus;
using DSharpPlus.Entities;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Unicord.Universal.Converters
{
    public class ChannelTypeTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TextChannelTemplate { get; set; }
        public DataTemplate VoiceChannelTemplate { get; set; }
        public DataTemplate CategoryTemplate { get; set; }
        public DataTemplate DMChannelTemplate { get; set; }
        public DataTemplate GroupChannelTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item)
        {          

            if (item is DiscordChannel channel)
            {
                switch (channel.Type)
                {
                    case ChannelType.Text:
                    case ChannelType.Store:
                    case ChannelType.Announcement:
                    case ChannelType.Unknown:
                        return TextChannelTemplate;
                    case ChannelType.Voice:
                        return VoiceChannelTemplate ?? TextChannelTemplate;
                    case ChannelType.Private:
                        return DMChannelTemplate;
                    case ChannelType.Group:
                        return GroupChannelTemplate ?? DMChannelTemplate;
                    case ChannelType.Category:
                        return CategoryTemplate;
                }
            }

            return TextChannelTemplate;
        }
    }
}

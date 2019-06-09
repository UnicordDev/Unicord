using System;
using DSharpPlus;
using DSharpPlus.Entities;
using Windows.UI.Xaml.Data;

namespace Unicord.Universal.Converters
{
    class ChannelSymbolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DiscordChannel c)
            {
                if (c.IsNSFW)
                {
                    return "\xE7BA";
                }

                var type = c.Type;
                switch (type)
                {
                    case ChannelType.Text:
                        return "\xE8BD";
                    case ChannelType.Voice:
                        return "\xE767";
                    case ChannelType.News:
                        return "\xF000";
                    case ChannelType.Store:
                        return "\xE719";
                    default:
                        return "";
                }
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

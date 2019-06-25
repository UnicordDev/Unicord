using System;
using System.Linq;
using DSharpPlus;
using DSharpPlus.Entities;
using WamWooWam.Core;
using Windows.UI.Xaml.Data;

namespace Unicord.Universal.Converters
{
    class DMNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DiscordDmChannel c)
            {
                if (c.Type == ChannelType.Private)
                {
                    return c.Recipient.DisplayName;
                }

                if (c.Type == ChannelType.Group)
                {
                    return c.Name ?? Strings.NaturalJoin(c.Recipients.Values.Select(r => r.DisplayName));
                }
            }

            return "I'm sorry what the fuck.";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
}

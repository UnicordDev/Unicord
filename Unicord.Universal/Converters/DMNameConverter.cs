using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WamWooWam.Core;
using Windows.UI.Xaml.Data;

namespace Unicord.Universal.Converters
{
    class DMNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if(value is DiscordDmChannel c)
            {
                if(c.Type == ChannelType.Private)
                {
                    return c.Recipient.DisplayName;
                }

                if(c.Type == ChannelType.Group)
                {
                    return c.Name ?? Strings.NaturalJoin(c.Recipients.Select(r => r.DisplayName));
                }
            }

            return "I'm sorry what the fuck.";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
}

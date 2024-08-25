using System;
using System.Diagnostics;
using System.Linq;
using DSharpPlus;
using DSharpPlus.Entities;
using Unicord.Universal.Models.Channels;
using WamWooWam.Core;
using Windows.UI.Xaml.Data;

namespace Unicord.Universal.Converters
{
    class DMNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is ChannelViewModel vm)
                value = vm.Channel;

            if (value is DiscordDmChannel c)
            {
                if (c.Type == ChannelType.Private)
                {
                    return c.Recipients[0].DisplayName;
                }

                if (c.Type == ChannelType.Group)
                {
                    return c.Name ?? Strings.NaturalJoin(c.Recipients.Select(r => r.DisplayName));
                }
            }

            if (value is DiscordChannel ch)
            {
                return ch.Name; // shh
            }

            Debug.Assert(false);
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
}

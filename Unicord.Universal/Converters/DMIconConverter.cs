using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace Unicord.Universal.Converters
{
    class DMIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DiscordDmChannel c)
            {
                if (c.Type == ChannelType.Private)
                {
                    return c.Recipient.NonAnimatedAvatarUrl;
                }

                if (c.Type == ChannelType.Group)
                {
                    if (c.IconUrl != null)
                    {
                        return c.IconUrl;
                    }
                }
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
}

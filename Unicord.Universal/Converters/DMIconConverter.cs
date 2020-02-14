using System;
using DSharpPlus;
using DSharpPlus.Entities;
using Windows.UI.Xaml.Data;

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
                    return c.Recipients[0].GetAvatarUrl(ImageFormat.Png, 64);
                }

                if (c.Type == ChannelType.Group)
                {
                    if (c.IconUrl != null)
                    {
                        return c.IconUrl + "?size=64";
                    }
                }
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
    }
}

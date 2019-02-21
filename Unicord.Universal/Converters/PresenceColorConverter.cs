using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Data;

namespace Unicord.Universal.Converters
{
    class PresenceColourConverter : IValueConverter
    {
        static Color _online = Color.FromArgb(255, 0x43, 0xb5, 0x81);

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var v = (DiscordPresence)value;
            if (v?.Activity?.ActivityType == ActivityType.Streaming)
                return Colors.Purple;

            switch (v?.Status ?? UserStatus.Offline)
            {
                case UserStatus.Offline:
                    return Colors.Gray;
                case UserStatus.Online:
                    return _online;
                case UserStatus.Idle:
                    return Colors.Orange;
                case UserStatus.DoNotDisturb:
                    return Colors.Red;
                case UserStatus.Invisible:
                    return Colors.Gray;
                default:
                    return Colors.Black;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    class PresenceTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if(!(value is DiscordActivity activity))
            {
                var v = value as DiscordPresence;
                activity = v?.Activity;
            }

            switch (activity?.ActivityType)
            {
                case ActivityType.Playing:
                    return "Playing";
                case ActivityType.Streaming:
                    return "Streaming";
                case ActivityType.ListeningTo:
                    return "Listening to";
                case ActivityType.Watching:
                    return "Watching";
                default:
                    break;
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

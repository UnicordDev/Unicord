using System;
using DSharpPlus.Entities;
using Windows.ApplicationModel.Resources;
using Windows.UI;
using Windows.UI.Xaml.Data;

namespace Unicord.Universal.Converters
{
    class PresenceColourConverter : IValueConverter
    {
        static readonly Color online = Color.FromArgb(255, 0x43, 0xb5, 0x81);

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var v = (DiscordPresence)value;
            if (v?.Activity?.ActivityType == ActivityType.Streaming)
            {
                return Colors.Purple;
            }

            switch (v?.Status ?? UserStatus.Offline)
            {
                case UserStatus.Offline:
                    return Colors.Gray;
                case UserStatus.Online:
                    return online;
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
        private ResourceLoader _strings;

        public PresenceTextConverter()
        {
            _strings = ResourceLoader.GetForViewIndependentUse("Converters");
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (!(value is DiscordActivity activity))
            {
                var v = value as DiscordPresence;
                activity = v?.Activity;
            }

            switch (activity?.ActivityType)
            {
                case ActivityType.Playing:
                    return string.Format(_strings.GetString("PlayingStatusFormat"), activity.Name);
                case ActivityType.Streaming:
                    return string.Format(_strings.GetString("StreamingStatusFormat"), activity.Name);
                case ActivityType.ListeningTo:
                    return string.Format(_strings.GetString("ListeningStatusFormat"), activity.Name);
                case ActivityType.Watching:
                    return string.Format(_strings.GetString("WatchingStatusFormat"), activity.Name);
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

using System;
using System.Linq;
using DSharpPlus.Entities;
using Windows.ApplicationModel.Resources;
using Windows.UI;
using Windows.UI.Xaml.Data;

namespace Unicord.Universal.Converters
{
    class PresenceColourConverter : IValueConverter
    {
        public Color Offline { get; set; }
        public Color Online { get; set; }
        public Color Idle { get; set; }
        public Color DoNotDisturb { get; set; }
        public Color Fallback { get; set; }

        public Color StreamingYouTube { get; set; }
        public Color StreamingTwitch { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var presence = (DiscordPresence)value;
            if (presence == null)
                return Offline;

            var streamActivity = presence.Activities?.FirstOrDefault(a => a?.ActivityType == ActivityType.Streaming) ?? presence.Activity;
            if (streamActivity != null && streamActivity.ActivityType == ActivityType.Streaming)
            {
                if (streamActivity.StreamUrl != null && Uri.TryCreate(streamActivity.StreamUrl, UriKind.Absolute, out var uri))
                {
                    if (uri.Host.ToLowerInvariant().Contains("youtu")) // should catch youtube and youtu.be
                        return StreamingYouTube;

                    return StreamingTwitch;
                }

                return StreamingTwitch;
            }

            switch (presence.Status)
            {
                case UserStatus.Invisible:
                case UserStatus.Offline:
                    return Offline;
                case UserStatus.Online:
                    return Online;
                case UserStatus.Idle:
                    return Idle;
                case UserStatus.DoNotDisturb:
                    return DoNotDisturb;
                default:
                    return Fallback;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    class PresenceGeometryConverter : IValueConverter
    {
        public string Offline { get; set; }
        public string Online { get; set; }
        public string Idle { get; set; }
        public string DoNotDisturb { get; set; }
        public string Fallback { get; set; }
        public string Streaming { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (!App.RoamingSettings.Read(Constants.SHOW_STATUS_GLYPHS, Constants.SHOW_STATUS_GLYPHS_DEFAULT))
            {
                return Fallback;
            }

            var presence = (DiscordPresence)value;
            if (presence == null)
                return Offline;

            var streamActivity = presence.Activities?.FirstOrDefault(a => a?.ActivityType == ActivityType.Streaming) ?? presence.Activity;
            if (streamActivity != null && streamActivity.ActivityType == ActivityType.Streaming)
            {
                return Streaming;
            }

            switch (presence.Status)
            {
                case UserStatus.Invisible:
                case UserStatus.Offline:
                    return Offline;
                case UserStatus.Online:
                    return Online;
                case UserStatus.Idle:
                    return Idle;
                case UserStatus.DoNotDisturb:
                    return DoNotDisturb;
                default:
                    return Fallback;
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
                case ActivityType.Custom:
                    return activity.CustomStatus.Name;
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

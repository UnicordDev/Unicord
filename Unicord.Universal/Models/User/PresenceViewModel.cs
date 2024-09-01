using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using Unicord.Universal.Converters;
using Unicord.Universal.Models.Emoji;
using Windows.ApplicationModel.Resources;
using Windows.UI;

namespace Unicord.Universal.Models.User
{
    public class PresenceViewModel : ViewModelBase
    {
        private static readonly ResourceLoader strings
            = ResourceLoader.GetForViewIndependentUse("Converters");

        private EmojiViewModel emoji;
        private string statusTitle;
        private string statusText;

        protected ulong userId;
        private bool empty;

        public PresenceViewModel(DiscordUser user, ViewModelBase parent)
            : base(parent)
        {
            userId = user.Id;
            OnPresenceUpdated();
        }

        public DiscordPresence Presence
            => discord.Presences.TryGetValue(userId, out var presence) ? presence : null;

        public UserStatus Status
            => Presence?.Status ?? UserStatus.Offline;

        // 
        // Presences are a bit weird, because we have so many of them and they tend to be tied to a user
        // it's the responsibility of the UserViewModel to notify of presence changes
        //
        internal void OnPresenceUpdated()
        {
            if (Presence == null)
            {
                HasActivity = false;
                return;
            }

            InvokePropertyChanged(nameof(Colour));
            InvokePropertyChanged(nameof(PresenceGeometry));
            InvokePropertyChanged(nameof(Status));

            var activity = Presence.Activities?.Where(p => p != null)
                .OrderByDescending(p => p.ActivityType)
                .FirstOrDefault();

            HasActivity = activity != null;
            if (activity == null) return;

            switch (activity.ActivityType)
            {
                case ActivityType.Playing:
                    CondensedTitle = strings.GetString("PlayingStatus");
                    CondensedText = activity.Name;
                    break;
                case ActivityType.Streaming:
                    CondensedTitle = strings.GetString("StreamingStatus");
                    CondensedText = activity.RichPresence?.Details ?? activity.Name;
                    break;
                case ActivityType.ListeningTo:
                    CondensedTitle = strings.GetString("ListeningStatus");
                    CondensedText = activity.Name;
                    break;
                case ActivityType.Watching:
                    CondensedTitle = strings.GetString("WatchingStatus");
                    CondensedText = activity.Name;
                    break;
                case ActivityType.Custom:
                    {
                        var custom = activity.CustomStatus;
                        if (!string.IsNullOrWhiteSpace(custom.Name))
                        {
                            // swapped so we have the text in not-bold
                            CondensedText = null;
                            CondensedTitle = custom.Name;
                        }

                        Emoji = new EmojiViewModel(custom.Emoji);
                        break;
                    }
                default:
                    break;
            }
        }

        // TODO: these properties suck
        public Color Colour
        {
            get
            {
                var converter = (PresenceColourConverter)App.Current.Resources["PresenceColourConverter"];
                return (Color)converter.Convert(Presence, typeof(Color), null, null);
            }
        }

        public string PresenceGeometry
        {
            get
            {
                var converter = (PresenceGeometryConverter)App.Current.Resources["PresenceGeometryConverter"];
                return (string)converter.Convert(Presence, typeof(string), null, null);
            }
        }

        public bool HasActivity { get => empty; private set => OnPropertySet(ref empty, value); }
        public EmojiViewModel Emoji { get => emoji; private set => OnPropertySet(ref emoji, value); }
        public string CondensedTitle { get => statusTitle; private set => OnPropertySet(ref statusTitle, value); }
        public string CondensedText { get => statusText; private set => OnPropertySet(ref statusText, value); }
    }
}

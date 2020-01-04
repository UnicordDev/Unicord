using System;
using DSharpPlus;
using DSharpPlus.Entities;
using static Unicord.Constants;

namespace Unicord.Universal.Models
{
    class MessagingSettingsModel : PropertyChangedBase
    {
        public MessagingSettingsModel()
        {
            var user = new MockUser("ExampleUser", "ABCD");
            var channel = new MockChannel("text", ChannelType.Text, "This is an example channel.");
            ExampleMessage = new MockMessage("This is an example message!", user, channel, DateTime.Now.Subtract(TimeSpan.FromMinutes(3)));
        }

        public DiscordMessage ExampleMessage { get; set; }

        public bool EnableSpoilers
        {
            get => App.RoamingSettings.Read(ENABLE_SPOILERS, true);
            set => App.RoamingSettings.Save(ENABLE_SPOILERS, value);
        }

        public int TimestampStyle
        {
            get => (int)App.RoamingSettings.Read(TIMESTAMP_STYLE, Unicord.TimestampStyle.Absolute);
            set
            {
                App.RoamingSettings.Save(TIMESTAMP_STYLE, (TimestampStyle)value);
            }
        }

        public bool AutoPlayGifs
        {
            get => App.RoamingSettings.Read(GIF_AUTOPLAY, true);
            set => App.RoamingSettings.Save(GIF_AUTOPLAY, value);
        }

        public bool WarnUnsafeLinks
        {
            get => App.RoamingSettings.Read(WARN_UNSAFE_LINKS, true);
            set => App.RoamingSettings.Save(WARN_UNSAFE_LINKS, value);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Unicord.Universal.Models;
using static Unicord.Constants;

namespace Unicord.Universal.Pages.Settings
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
            get => (int)App.RoamingSettings.Read("TimestampStyle", Unicord.TimestampStyle.Absolute);
            set
            {
                App.RoamingSettings.Save("TimestampStyle", (TimestampStyle)value);
            }
        }

        public bool AutoPlayGifs
        {
            get => App.RoamingSettings.Read("AutoPlayGifs", true);
            set => App.RoamingSettings.Save("AutoPlayGifs", value);
        }
    }
}

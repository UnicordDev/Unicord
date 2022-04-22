using System.Collections.Generic;
using DSharpPlus.Entities;
using Windows.Media.Transcoding;
using static Unicord.Constants;

namespace Unicord.Universal.Models
{
    public class RootSettingsModel : ViewModelBase
    {
        public DiscordUser CurrentUser => 
            App.Discord.CurrentUser;

        public string AccountItemImage =>
            App.Discord.CurrentUser.AvatarUrl;

        public string AccountDisplayName =>
            App.Discord.CurrentUser.Username;
    }
}

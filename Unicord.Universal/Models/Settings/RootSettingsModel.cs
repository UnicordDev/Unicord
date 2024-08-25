using System.Collections.Generic;
using System.Reflection;
using DSharpPlus.Entities;
using Microsoft.Toolkit.Uwp.Helpers;
using Unicord.Universal.Extensions;
using Windows.ApplicationModel;
using Windows.Media.Transcoding;
using static Unicord.Constants;

namespace Unicord.Universal.Models
{
    public class RootSettingsModel : ViewModelBase
    {
        public DiscordUser CurrentUser =>
            App.Discord.CurrentUser;

        public string AccountItemImage =>
            App.Discord.CurrentUser.GetAvatarUrl(256);

        public string AccountDisplayName =>
            App.Discord.CurrentUser.Username;

        public string DisplayVersion
        {
            get
            {
                var gitSha = "";
                var versionedAssembly = typeof(VersionHelper).Assembly;
                var attribute = versionedAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                var idx = -1;
                if (attribute != null && (idx = attribute.InformationalVersion.IndexOf('+')) != -1)
                {
                    gitSha = "-" + attribute.InformationalVersion.Substring(idx + 1, 7);
                }

                return $"v{Package.Current.Id.Version.ToFormattedString()}{gitSha}";
            }
        }
    }
}

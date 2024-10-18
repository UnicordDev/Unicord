using System.Reflection;
using DSharpPlus.Entities;
using Microsoft.Toolkit.Uwp.Helpers;
using Unicord.Universal.Extensions;
using Windows.ApplicationModel;

namespace Unicord.Universal.Models
{
    public class RootSettingsModel : ViewModelBase
    {
        public DiscordUser CurrentUser =>
            discord.CurrentUser;

        public string AccountItemImage =>
            discord.CurrentUser.GetAvatarUrl(256);

        public string AccountDisplayName =>
            discord.CurrentUser.Username;

        public string DisplayVersion
        {
            get
            {
                var gitSha = "";
                var versionedAssembly = typeof(RootSettingsModel).Assembly;
                var attribute = versionedAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                var idx = -1;
                if (attribute != null && (idx = attribute.InformationalVersion.IndexOf('+')) != -1)
                {
                    gitSha = "-" + attribute.InformationalVersion.Substring(idx + 1, 7);
                }

                return $"v{Package.Current.Id.Version.ToFormattedString(3)}{gitSha}";
            }
        }
    }
}

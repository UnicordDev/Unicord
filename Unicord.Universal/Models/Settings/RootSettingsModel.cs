using System.Reflection;
using DSharpPlus.Entities;
using Microsoft.Toolkit.Uwp.Helpers;
using Unicord.Universal.Extensions;
using Windows.ApplicationModel;

namespace Unicord.Universal.Models
{
    public class RootSettingsModel : ViewModelBase
    {
        private static readonly bool _isWindows11 = SystemInformation.Instance.OperatingSystemVersion.Build >= 22000;

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

                return $"{Package.Current.Id.Version.ToFormattedString(3)}{gitSha}";
            }
        }

        public string NotificationIconGlyph
            => _isWindows11 ? "\uEA8F" : "\uE91C";
    }
}

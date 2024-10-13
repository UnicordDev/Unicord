using DSharpPlus;
using DSharpPlus.Entities;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Unicord.Universal.Controls.Messages;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Management.Deployment;
using Windows.System;
using Windows.UI.Xaml;
using static Unicord.Constants;

namespace Unicord.Universal.Models
{
    public class MessageStyle
    {
        public string Key { get; set; }
        public Style Value { get; set; }
        public DiscordMessage ExampleMessage { get; set; }
    }

    internal class TimestampStyleModel
    {
        internal TimestampStyleModel(TimestampStyle style, DateTime timestamp)
        {
            Style = style;
            Timestamp = timestamp;
        }

        internal TimestampStyle Style { get; set; }
        internal DateTime Timestamp { get; set; }
    }

    internal class MessagingSettingsModel : ViewModelBase
    {
        private bool _canUseWebp = false;

        private const string WEBP_IMAGE_EXTENSIONS_PRODUCTID = "9PG2DK419DRG";
        private const string WEBP_IMAGE_EXTENSIONS_PACKAGEFAMILYNAME = "Microsoft.WebpImageExtension_8wekyb3d8bbwe";

        private static Uri WebPStoreUri = new Uri($"ms-windows-store://pdp/?ProductId={WEBP_IMAGE_EXTENSIONS_PRODUCTID}");

        public MessagingSettingsModel()
        {
            CanUseWebP = Tools.HasWebPSupport();
            OpenWebPStoreLinkCommand = new AsyncRelayCommand(async () => await Launcher.LaunchUriAsync(WebPStoreUri));
        } 

        public bool EnableSpoilers
        {
            get => App.RoamingSettings.Read(ENABLE_SPOILERS, true);
            set => App.RoamingSettings.Save(ENABLE_SPOILERS, value);
        }

        public int TimestampStyle
        {
            get => (int)App.RoamingSettings.Read(TIMESTAMP_STYLE, (int)Unicord.TimestampStyle.Absolute);
            set => App.RoamingSettings.Save(TIMESTAMP_STYLE, (int)value);
        }

        public TimestampStyleModel[] TimestampStyles { get; } = new[]
        {
            new TimestampStyleModel(Unicord.TimestampStyle.Relative, DateTime.Now.AddMinutes(-3)),
            new TimestampStyleModel(Unicord.TimestampStyle.Absolute, DateTime.Now.AddMinutes(-3)),
            new TimestampStyleModel(Unicord.TimestampStyle.Both, DateTime.Now.AddMinutes(-3)),
        };

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

        public bool ShowHugeEmoji
        {
            get => App.RoamingSettings.Read(SHOW_HUGE_EMOJI, SHOW_HUGE_EMOJI_DEFAULT);
            set => App.RoamingSettings.Save(SHOW_HUGE_EMOJI, value);
        }

        public bool ShowStatusGlyphs
        {
            get => App.RoamingSettings.Read(SHOW_STATUS_GLYPHS, SHOW_STATUS_GLYPHS_DEFAULT);
            set => App.RoamingSettings.Save(SHOW_STATUS_GLYPHS, value);
        }

        public bool AdjustRoleColours
        {
            get => App.RoamingSettings.Read(ADJUST_ROLE_COLOURS, ADJUST_ROLE_COLOURS_DEFAULT);
            set => App.RoamingSettings.Save(ADJUST_ROLE_COLOURS, value);
        }

        public double MinimumContrast
        {
            get => App.RoamingSettings.Read(MINIMUM_CONTRAST, MINIMUM_CONTRAST_DEFAULT);
            set => App.RoamingSettings.Save(MINIMUM_CONTRAST, value);
        }

        public bool EnableWebP
        {
            get => App.LocalSettings.Read(ENABLE_WEBP, ENABLE_WEBP_DEFAULT);
            set => App.LocalSettings.Save(ENABLE_WEBP, value);
        }

        public bool CanUseWebP { get; }
        public ICommand OpenWebPStoreLinkCommand { get; }
    }
}

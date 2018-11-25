using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unicord
{
    public static partial class Constants
    {

#if DEBUG
        public const string MAIN_URL = "http://localhost:8000/";
#else
        public const string MAIN_URL = "https://dwpf.wankerr.com/";
#endif

        public const string APP_USER_MODEL_ID = "com.wankerr.Unicord";
        public const string PLUGIN_IPC_URI = "net.pipe://localhost/org.wamwoowam.unicord/plugins";

        public const string UPDATE_BASE_URL = MAIN_URL + "update/";

        public const string OS_WARNING_DISMISSED = "OSWarningDismissed";

        public const string ALLOW_MULTIPLE_CHANNEL_WINDOWS = "MultipleChannelWindows";
        public const string ALLOW_MULTIPLE_GUILD_WINDOWS = "MultipleGuildWindows";
        public const string ALLOW_MULTIPLE_CHANNEL_PAGES = "MultipleChannelPages";

        public const string USE_CUSTOM_TITLE_BAR = "UseCustomTitleBar";
        public const string NO_CACHE_CONTEXT_MENUS = "NoCacheContextMenus";

        public const string USE_LIGHT_THEME = "LightTheme";
        public const string USE_DISCORD_ACCENT_COLOUR = "DiscordAccent";
        public const string CUSTOM_ACCENT_COLOUR = "CustomAccent";

        public const string MINI_MODE_POSITIONS = "MiniModePositions";
        public const string MINI_MODE_SNAP_ENABLED = "MiniModeSnap";

        public const string DISABLE_TELEMETRY = "DisableTelemetry";

        public const string AUDIOPHILE_MODE = "AudiophileMode";
        public const string AUTO_TRANSCODE_MEDIA = "AutoTranscodeMedia";
    }

    public enum MediaTranscodeOptions
    {
        Always,
        WhenNeeded,
        Never
    }
}

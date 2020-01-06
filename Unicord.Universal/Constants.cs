using System;

namespace Unicord
{
    public static partial class Constants
    {
        public const string MAIN_URL = "https://wamwoowam.co.uk/unicord/";

        public const string TOKEN_IDENTIFIER = "Unicord_Token";
        public const string APP_USER_MODEL_ID = "com.wankerr.Unicord";

        public const string ENABLE_ANALYTICS = "EnableAnalytics";
        public const string SYNC_CONTACTS = "SyncContacts";
        public const string ENABLE_SPOILERS = "EnableSpoilers";
        public const string AUTO_TRANSCODE_MEDIA = "AutoTranscodeMedia";
        public const string WARN_UNSAFE_LINKS = "WarnUnsafeLinks";

        public const string VIDEO_BITRATE = "VideoBitrate";
        public const string VIDEO_WIDTH = "VideoWidth";
        public const string VIDEO_HEIGHT = "VideoHeight";

        public const string AUDIO_BITRATE = "AudioBitrate";
        public const string AUDIO_SAMPLERATE = "AudioSampleRate";
        public const string VIDEO_PROCESSING = "VideoProcessingAlgorithm";

        public const string VERIFY_LOGIN = "HelloForLogin";
        public const string VERIFY_NSFW = "HelloForNSFW";
        public const string VERIFY_SETTINGS = "HelloForSettings";
        public const string AUTHENTICATION_TIME = "AuthenticationTime";

        public const string GIF_AUTOPLAY = "AutoPlayGifs";
        public const string SAVE_CAPTURED_PHOTOS = "SavedPhotos";
        public const string TIMESTAMP_STYLE = "TimestampStyle";

        [Obsolete("Use new theme system (SELECTED_THEME_NAMES) instead.")]
        public const string SELECTED_THEME_NAME = "SelectedThemeName";
        public const string SELECTED_THEME_NAMES = "SelectedThemeNames";
        public const string REQUESTED_COLOUR_SCHEME = "RequestedTheme";
        public const string THEME_FOLDER_NAME = "Themes";
        public const string THEME_METADATA_NAME = "theme.json";
    }

    public enum MediaTranscodeOptions
    {
        Always,
        WhenNeeded,
        Never
    }

    public enum TimestampStyle
    {
        Relative,
        Absolute,
        Both
    }
}

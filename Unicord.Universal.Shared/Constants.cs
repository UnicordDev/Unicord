﻿using System;
using System.Threading;

namespace Unicord
{
    internal static partial class Constants
    {
        public const string MAIN_URL = "https://wamwoowam.co.uk/unicord/";

        public const string TOKEN_IDENTIFIER = "Unicord_Token_New";
        public const string APP_USER_MODEL_ID = "com.wankerr.Unicord";

        public const string ENABLE_ANALYTICS = "EnableAnalytics";
        public const string SYNC_CONTACTS = "SyncContacts";
        public const string ENABLE_SPOILERS = "EnableSpoilers";
        public const string AUTO_TRANSCODE_MEDIA = "AutoTranscodeMedia_1";
        public const string WARN_UNSAFE_LINKS = "WarnUnsafeLinks";

        public const string VIDEO_BITRATE = "VideoBitrate";
        public const uint DEFAULT_VIDEO_BITRATE = 1_115_000u;

        public const string VIDEO_WIDTH = "VideoWidth";
        public const int DEFAULT_VIDEO_WIDTH = 854;

        public const string VIDEO_HEIGHT = "VideoHeight";
        public const int DEFAULT_VIDEO_HEIGHT = 480;

        public const string AUDIO_BITRATE = "AudioBitrate";
        public const uint DEFAULT_AUDIO_BITRATE = 192 * 1000;

        public const string AUDIO_SAMPLERATE = "AudioSampleRate";
        public const uint DEFAULT_AUDIO_SAMPLERATE = 48000u;

        public const string VIDEO_PROCESSING = "VideoProcessingAlgorithm_1";

        public const string SAVE_PHOTOS = "SavePhotos";
        public const bool DEFAULT_SAVE_PHOTOS = true;

        public const string VIDEO_PRESERVE_FRAMERATE = "PreserveFrameRate";
        public const bool DEFAULT_VIDEO_PRESERVE_FRAMERATE = true;

        public const string VERIFY_LOGIN = "HelloForLogin";
        public const string VERIFY_NSFW = "HelloForNSFW";
        public const string VERIFY_SETTINGS = "HelloForSettings";
        public const string AUTHENTICATION_TIME = "AuthenticationTime";

        public const string GIF_AUTOPLAY = "AutoPlayGifs";
        public const string SAVE_CAPTURED_PHOTOS = "SavedPhotos";
        public const string TIMESTAMP_STYLE = "TimestampStyle_1";

        public const string BACKGROUND_NOTIFICATIONS = "BackgroundNotifications";

        [Obsolete("Use new theme system (SELECTED_THEME_NAMES) instead.")]
        public const string SELECTED_THEME_NAME = "SelectedThemeName";

        public const string SELECTED_THEME_NAMES = "SelectedThemeNames";
        public const string AVAILABLE_THEME_NAMES = "AvailableThemeNames";
        public const string REQUESTED_COLOUR_SCHEME = "RequestedTheme_1";
        public const string THEME_FOLDER_NAME = "Themes";
        public const string THEME_METADATA_NAME = "theme.json";

        public const string MESSAGE_STYLE_KEY = "MessageStyleKey";
        public const string MESSAGE_STYLE_DEFAULT = "DefaultMessageControlStyle";

        public const string TOAST_BACKGROUND_TASK_NAME = "ToastBackgroundTask";
        public const string PERIODIC_BACKGROUND_TASK_NAME = "PeriodicNotificationsTask";

        public const string SHOW_HUGE_EMOJI = "ShowHugeEmoji";
        public const bool   SHOW_HUGE_EMOJI_DEFAULT = true;

        public const string ADJUST_ROLE_COLOURS = "AdjustRoleColours";
        public const bool   ADJUST_ROLE_COLOURS_DEFAULT = true;

        public const string SHOW_STATUS_GLYPHS = "ShowStatusGlyphs";
        public const bool   SHOW_STATUS_GLYPHS_DEFAULT = true;

        public const string MINIMUM_CONTRAST = "MinimumContrast";
        public const double MINIMUM_CONTRAST_DEFAULT = 3.5;

        public const string ENABLE_WEBP = "EnableWebp"; 
        public const bool   ENABLE_WEBP_DEFAULT = false; // slow as balls

        public const string ENABLE_NOTIFICATIONS = "EnableNotifications";
        public const bool   ENABLE_NOTIFICATIONS_DEFAULT = true;

        public const string ENABLE_DESKTOP_NOTIFICAITONS = "EnableDesktopNotifications";
        public const bool   ENABLE_DESKTOP_NOTIFICAITONS_DEFAULT = true;

        public const string ENABLE_BADGE_COUNT = "EnableBadgeCount";
        public const bool   ENABLE_BADGE_COUNT_DEFAULT = true;

        public const string ENABLE_BADGE_UNREAD = "EnableBadgeUnread";
        public const bool   ENABLE_BADGE_UNREAD_DEFAULT = true;

        public const string ENABLE_LIVE_TILES = "EnableLiveTiles";
        public const bool   ENABLE_LIVE_TILES_DEFAULT = true;

        // old resource keys for migration
        public const string TOKEN_IDENTIFIER_OLD = "Unicord_Token";
        public const string AUTO_TRANSCODE_MEDIA_OLD = "AutoTranscodeMedia";
        public const string VIDEO_PROCESSING_OLD = "VideoProcessingAlgorithm";
        public const string TIMESTAMP_STYLE_OLD = "TimestampStyle";
        public const string REQUESTED_COLOUR_SCHEME_OLD = "RequestedTheme";
    }

    internal enum MediaTranscodeOptions
    {
        Always,
        WhenNeeded,
        Never
    }

    internal enum TimestampStyle
    {
        Relative,
        Absolute,
        Both
    }
}

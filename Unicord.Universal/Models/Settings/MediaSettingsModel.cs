using System.Collections.Generic;
using DSharpPlus.Entities;
using Windows.Media.Transcoding;
using static Unicord.Constants;

namespace Unicord.Universal.Models
{
    public class MediaSettingsModel : ViewModelBase
    {
        private int[] _availableWidths = new[] { 256, 426, 640, 854, 1280, 1920 };
        private int[] _availableHeights = new[] { 144, 240, 360, 480, 720, 1080 };

        public int AutoTranscodeMedia
        {
            get => (int)App.RoamingSettings.Read(AUTO_TRANSCODE_MEDIA, (int)MediaTranscodeOptions.WhenNeeded);
            set => App.RoamingSettings.Save(AUTO_TRANSCODE_MEDIA, (int)value);
        }

        public int ProcessingAlgorithm
        {
            get => (int)App.RoamingSettings.Read(VIDEO_PROCESSING, (int)MediaVideoProcessingAlgorithm.Default);
            set => App.RoamingSettings.Save(VIDEO_PROCESSING, (int)value);
        }

        public int VideoBitrate
        {
            get => (int)(App.RoamingSettings.Read(VIDEO_BITRATE, DEFAULT_VIDEO_BITRATE) / 1000);
            set => App.RoamingSettings.Save(VIDEO_BITRATE, value * 1000);
        }

        public int AudioBitrate
        {
            get => (int)(App.RoamingSettings.Read(AUDIO_BITRATE, DEFAULT_AUDIO_BITRATE) / 1000);
            set => App.RoamingSettings.Save(AUDIO_BITRATE, value * 1000);
        }

        public int VideoWidth
        {
            get => App.RoamingSettings.Read(VIDEO_WIDTH, DEFAULT_VIDEO_WIDTH);
            set => App.RoamingSettings.Save(VIDEO_WIDTH, value);
        }

        public int VideoHeight
        {
            get => App.RoamingSettings.Read(VIDEO_HEIGHT, DEFAULT_VIDEO_HEIGHT);
            set => App.RoamingSettings.Save(VIDEO_HEIGHT, value);
        }

        public bool SavePhotos
        {
            get => App.RoamingSettings.Read(SAVE_PHOTOS, DEFAULT_SAVE_PHOTOS);
            set => App.RoamingSettings.Save(SAVE_PHOTOS, value);
        }

        public bool PreserveFrameRate
        {
            get => App.RoamingSettings.Read(VIDEO_PRESERVE_FRAMERATE, DEFAULT_VIDEO_PRESERVE_FRAMERATE);
            set => App.RoamingSettings.Save(VIDEO_PRESERVE_FRAMERATE, value);
        }

        public List<string> AvailableResolutions => new List<string> { "144p", "240p", "360p", "480p", "720p", "1080p" };
        public string Resolution
        {
            get => $"{VideoHeight}p";
            set
            {
                var index = AvailableResolutions.IndexOf(value);
                VideoWidth = _availableWidths[index];
                VideoHeight = _availableHeights[index];
            }
        }
    }
}

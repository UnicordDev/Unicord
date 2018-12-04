using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Transcoding;
using static Unicord.Constants;

namespace Unicord.Universal.Pages.Settings
{
    public class MediaSettingsModel : PropertyChangedBase
    {
        public int AutoTranscodeMedia
        {
            get => (int)App.RoamingSettings.Read(AUTO_TRANSCODE_MEDIA, MediaTranscodeOptions.WhenNeeded);
            set => App.RoamingSettings.Save(AUTO_TRANSCODE_MEDIA, (MediaTranscodeOptions)value);
        }

        public int ProcessingAlgorithm
        {
            get => (int)App.RoamingSettings.Read(VIDEO_PROCESSING, MediaVideoProcessingAlgorithm.MrfCrf444);
            set => App.RoamingSettings.Save(VIDEO_PROCESSING, (MediaTranscodeOptions)value);
        }

        public int VideoBitrate
        {
            get => App.RoamingSettings.Read(VIDEO_BITRATE, 1_150_000) / 1000;
            set => App.RoamingSettings.Save(VIDEO_BITRATE, value * 1000);
        }

        public int AudioBitrate
        {
            get => App.RoamingSettings.Read(AUDIO_BITRATE, 192);
            set => App.RoamingSettings.Save(AUDIO_BITRATE, value);
        }

        public List<string> AvailableResolutions => new List<string> { "256x144", "426x240", "640x360", "854x480", "1280x720", "1920x1080" };

        public int VideoWidth
        {
            get => App.RoamingSettings.Read(VIDEO_WIDTH, 854);
            set => App.RoamingSettings.Save(VIDEO_WIDTH, value);
        }

        public int VideoHeight
        {
            get => App.RoamingSettings.Read(VIDEO_HEIGHT, 480);
            set => App.RoamingSettings.Save(VIDEO_HEIGHT, value);
        }

        public bool SavePhotos
        {
            get => App.RoamingSettings.Read("SavePhotos", true);
            set => App.RoamingSettings.Save("SavePhotos", value);
        }

        public string Resolution
        {
            get => $"{VideoWidth}x{VideoHeight}";
            set
            {
                var split = value.Split('x');
                VideoWidth = int.Parse(split[0]);
                VideoHeight = int.Parse(split[1]);
            }
        }
    }
}

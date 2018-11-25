using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unicord.Universal.Pages.Settings
{
    public class MediaSettingsModel : PropertyChangedBase
    {
        public int AutoTranscodeMedia
        {
            get => (int)App.RoamingSettings.Read("AutoTranscodeMedia", MediaTranscodeOptions.WhenNeeded);
            set => App.RoamingSettings.Save("AutoTranscodeMedia", (MediaTranscodeOptions)value);
        }

        public double VideoBitrate
        {
            get => App.RoamingSettings.Read("VideoBitrate", 1_150_000) / 1000;
            set => App.RoamingSettings.Save("VideoBitrate", value * 1000);
        }

        public List<string> AvailableResolutions => new List<string> { "256x144", "426x240", "640x360", "854x480", "1280x720", "1920x1080" };

        public int VideoWidth
        {
            get => App.RoamingSettings.Read("VideoWidth", 854);
            set => App.RoamingSettings.Save("VideoWidth", value);
        }

        public int VideoHeight
        {
            get => App.RoamingSettings.Read("VideoHeight", 480);
            set => App.RoamingSettings.Save("VideoHeight", value);
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

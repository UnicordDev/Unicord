using System;
using DSharpPlus.Entities;

namespace Unicord.Universal.Models.Messages
{
    public class EmbedVideoViewModel : ViewModelBase
    {
        public EmbedVideoViewModel(DiscordEmbedVideo video, EmbedViewModel parent)
            : base(parent)
        {
            Type = parent.Type;
            NaturalWidth = video.Width;
            NaturalHeight = video.Height;
            Url = video.Url;
            Thumbnail = parent.Thumbnail;
            Provider = parent.ProviderName?.ToLowerInvariant() ?? "";
        }

        public string Type { get; }
        public EmbedImageViewModel Thumbnail { get; }
        public int NaturalWidth { get; }
        public int NaturalHeight { get; }
        public Uri Url { get; }
        public string Provider { get; }
    }
}

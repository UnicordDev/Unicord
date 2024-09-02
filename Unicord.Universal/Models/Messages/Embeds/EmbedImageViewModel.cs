using System;
using DSharpPlus.Entities;

namespace Unicord.Universal.Models.Messages
{
    public class EmbedImageViewModel : ViewModelBase
    {
        public EmbedImageViewModel(DiscordEmbedImage image, EmbedViewModel parent)
            : base(parent)
        {
            NaturalWidth = image.Width;
            NaturalHeight = image.Height;
            Source = image.ProxyUrl.ToUri();
            Url = image.Url.ToString();
        }

        public EmbedImageViewModel(DiscordEmbedThumbnail image, EmbedViewModel parent)
            : base(parent)
        {
            NaturalWidth = image.Width;
            NaturalHeight = image.Height;
            Source = image.ProxyUrl.ToUri();
            Url = image.Url.ToString();
        }

        public int NaturalWidth { get; }
        public int NaturalHeight { get; }
        public Uri Source { get; }
        public string Url { get; }
    }
}

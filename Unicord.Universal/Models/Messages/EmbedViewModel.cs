using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.Net;
using Unicord.Universal.Extensions;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

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

    public class EmbedAuthorViewModel : ViewModelBase
    {
        public EmbedAuthorViewModel(DiscordEmbedAuthor author, EmbedViewModel parent)
            : base(parent)
        {
            Name = author.Name;
            IconUrl = author.ProxyIconUrl?.ToUri();
        }

        public string Name { get; }
        public Uri IconUrl { get; }
        public bool HasIconUrl
            => IconUrl != null;
    }

    public class EmbedFooterViewModel : ViewModelBase
    {
        public EmbedFooterViewModel(DiscordEmbedFooter footer, EmbedViewModel parent)
            : base(parent)
        {
            Text = footer.Text;
            IconUrl = footer.ProxyIconUrl?.ToUri();
        }

        public string Text { get; }
        public Uri IconUrl { get; }
        public bool HasIconUrl
            => IconUrl != null;
    }

    public class EmbedFieldViewModel : ViewModelBase
    {
        public EmbedFieldViewModel(DiscordEmbedField field, EmbedViewModel parent)
            : base(parent)
        {
            Title = field.Name;
            Text = field.Value;
            ColumnSpan = field.Inline ? 1 : 3;
            Channel = parent.Channel;
        }

        public string Title { get; }
        public string Text { get; }
        public int ColumnSpan { get; }
        public DiscordChannel Channel { get; }
    }

    public class EmbedViewModel : ViewModelBase
    {
        private DiscordEmbed embed;
        private DiscordEmbed[] otherImages;
        private MessageViewModel messageViewModel;

        public EmbedViewModel(DiscordEmbed embed, DiscordEmbed[] otherImages, ViewModelBase parent)
            : base(parent)
        {
            this.messageViewModel = parent as MessageViewModel;
            this.Update(embed, otherImages);
        }

        // todo: viewmodelify when possible
        public DiscordChannel Channel
            => this.messageViewModel?.Channel?.Channel;
        public bool IsRawImage =>
            embed.Type == "image" && embed.Thumbnail != null;

        public bool IsRawVideo =>
            embed.Type == "gifv" || (embed.Type == "video" && embed.Video.Url.Host == "cdn.discordapp.com");

        public bool IsVideo =>
            !IsRawVideo && embed.Type == "video";

        public bool IsRichEmbed
            => !IsRawImage && !IsRawVideo;

        public string Type
            => embed.Type;

        public string Title
            => embed.Title;
        public bool HasTitle
            => !string.IsNullOrWhiteSpace(Title);

        public string Description
            => embed.Description;
        public bool HasDescription
            => embed.Type != "video" && !string.IsNullOrWhiteSpace(Description);

        public EmbedFieldViewModel[] Fields { get; private set; }

        public bool HasFields
            => Fields.Length > 0;

        public Uri Url
            => embed.Url;

        public SolidColorBrush Color
            => embed.Color.HasValue && embed.Color.Value.Value != 0 ?
                new SolidColorBrush(embed.Color.Value.ToColor())
            : App.Current.Resources["AccentFillColorDefaultBrush"] as SolidColorBrush; // FallbackValue just doesn't work lol

        public bool HasProvider
            => embed.Provider != null;
        public string ProviderName
            => embed.Provider?.Name;
        public Uri ProviderUrl
            => embed.Provider?.Url;

        public bool HasAuthor
            => embed.Author != null;

        public EmbedAuthorViewModel Author
            => embed.Author != null ? new EmbedAuthorViewModel(embed.Author, this) : null;

        public bool HasVideo
            => embed.Video != null;
        public EmbedVideoViewModel Video
            => embed.Video != null ? new EmbedVideoViewModel(embed.Video, this) : null;

        public bool HasFooter
            => embed.Footer != null;

        public EmbedFooterViewModel Footer
            => embed.Footer != null ? new EmbedFooterViewModel(embed.Footer, this) : null;

        /// <summary>
        /// Show the left hand side thumbanil if:
        ///     - We aren't a video
        ///     - We aren't an image
        ///     - We aren't an article
        ///     - We have a Thumbnail object
        ///     - We're not showing any other images
        /// </summary>
        public bool HasSmallThumbnail
            => (embed.Video == null && embed.Type != "article")
                && embed.Thumbnail != null
                && !string.IsNullOrWhiteSpace(embed.Thumbnail.ProxyUrl.ToString())
                && otherImages.Length == 0;

        public bool HasLargeThumbnail
            => (embed.Video == null && embed.Type == "article")
                && embed.Thumbnail != null
                && !string.IsNullOrWhiteSpace(embed.Thumbnail.ProxyUrl.ToString())
                && otherImages.Length == 0;

        public EmbedImageViewModel Image
            => embed.Image != null ? new EmbedImageViewModel(embed.Image, this) : null;
        public EmbedImageViewModel Thumbnail
            => embed.Thumbnail != null ? new EmbedImageViewModel(embed.Thumbnail, this) : null;

        internal void Update(DiscordEmbed embed, IEnumerable<DiscordEmbed> otherImages)
        {
            this.embed = embed;
            this.otherImages = otherImages.ToArray();
            this.Fields = embed.Fields?.Select(f => new EmbedFieldViewModel(f, this)).ToArray() ?? Array.Empty<EmbedFieldViewModel>();

            InvokePropertyChanged("");
        }
    }
}

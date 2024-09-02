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

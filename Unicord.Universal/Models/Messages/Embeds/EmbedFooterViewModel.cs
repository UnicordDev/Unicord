using System;
using DSharpPlus.Entities;

namespace Unicord.Universal.Models.Messages
{
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
}

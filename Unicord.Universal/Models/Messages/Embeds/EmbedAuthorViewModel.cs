using System;
using DSharpPlus.Entities;

namespace Unicord.Universal.Models.Messages
{
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
}

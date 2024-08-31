using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Entities;

namespace Unicord.Universal.Models.Emoji
{
    public readonly struct EmojiViewModel
    {
        public EmojiViewModel(DiscordEmoji emoji)
        {
            Emoji = emoji;
        }

        public DiscordEmoji Emoji { get; }

        public readonly string Url =>
            this.Emoji.Id == 0 ? "" : this.Emoji.Url;

        public readonly string Name
            => this.Emoji.Name;
    }
}

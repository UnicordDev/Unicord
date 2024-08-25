using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DSharpPlus;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using Unicord.Universal.Extensions;

namespace Unicord.Universal.Misc
{
    public class EmojiHeader
    {
        public string Name { get; set; }
        public string IconCharacter { get; set; }
        public string IconUrl { get; set; }
    }

    public class EmojiGroup : IGrouping<EmojiHeader, DiscordEmoji>
    {
        private List<DiscordEmoji> _emojis;

        public EmojiGroup(DiscordGuild guild, IEnumerable<DiscordEmoji> emojis)
        {
            Key = new EmojiHeader() { Name = guild.Name, IconUrl = guild.GetIconUrl(32) };
            _emojis = emojis.ToList();
        }

        public EmojiGroup(string category, IEnumerable<NeoSmart.Unicode.SingleEmoji> emojis)
        {
            var first = emojis.First();
            Key = new EmojiHeader() { Name = category, IconCharacter = first.Sequence.AsString };
            _emojis = emojis.Select(e => DiscordEmoji.FromUnicode(e.Sequence.AsString)).ToList();
        }

        public EmojiHeader Key { get; }

        public IEnumerator<DiscordEmoji> GetEnumerator() => _emojis.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _emojis.GetEnumerator();
    }
}

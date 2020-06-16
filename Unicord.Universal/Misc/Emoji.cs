using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DSharpPlus.Entities;
using Newtonsoft.Json;

namespace Unicord.Universal.Misc
{
    public class Emoji
    {
        [JsonProperty("char")]
        public string Char { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }
    }

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
            Key = new EmojiHeader() { Name = guild.Name, IconUrl = guild.IconUrl };
            _emojis = emojis.ToList();
        }

        public EmojiGroup(string category, IEnumerable<Emoji> emojis)
        {
            var first = emojis.First();
            Key = new EmojiHeader() { Name = category, IconCharacter = first.Char };
            _emojis = emojis.Select(e => DiscordEmoji.FromUnicode(e.Char)).ToList();
        }

        public EmojiHeader Key { get; }

        public IEnumerator<DiscordEmoji> GetEnumerator() => _emojis.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _emojis.GetEnumerator();
    }
}

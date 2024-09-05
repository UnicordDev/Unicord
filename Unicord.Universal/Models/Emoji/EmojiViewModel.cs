using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using NeoSmart.Unicode;

namespace Unicord.Universal.Models.Emoji
{
    public readonly struct EmojiViewModel
    {
        private readonly DiscordEmoji _emoji;

        public EmojiViewModel(DiscordComponentEmoji emoji) : this()
        {
            if (emoji != null)
            {
                if (emoji.Id != 0)
                {
                    if (DiscordEmoji.TryFromGuildEmote(App.Discord, emoji.Id, out var discordEmoji))
                    {
                        _emoji = discordEmoji;
                        Name = discordEmoji.Name;
                        Url = discordEmoji.Url + "?size=32";
                        IsValid = true;
                    }
                    else
                    {
                        Name = emoji.Name;
                        Url = $"https://cdn.discordapp.com/emojis/{emoji.Id.ToString(CultureInfo.InvariantCulture)}.png?size=32";
                    }
                }
                else
                {
                    Name = emoji.Name;
                    Unicode = emoji.Name;
                }

                IsValid = true;
            }
        }

        public EmojiViewModel(DiscordEmoji emoji)
        {
            //Emoji = emoji;
            _emoji = emoji;
            Name = emoji.Id != 0 ? emoji.Name : emoji.GetDiscordName();
            Unicode = emoji.Id == 0 ? emoji.Name : null;
            IsAvailable = emoji.IsAvailable;
            Url = emoji.Id != 0 ? emoji.Url + "?size=32" : null;
            IsValid = true;
        }

        public EmojiViewModel(SingleEmoji emoji)
        {
            _emoji = null;
            Unicode = emoji.Sequence.AsString;

            if (DiscordEmoji.DiscordNameLookup.TryGetValue(Unicode, out var name))
                Name = $":{name}:";
            else
                Name = "";

            IsAvailable = true;
            IsValid = true;
            Url = null;
        }

        public EmojiViewModel(ulong id, string text, bool isAnimated) : this()
        {
            Name = text;
            Unicode = "";
            Url = isAnimated
                    ? $"https://cdn.discordapp.com/emojis/{id.ToString(CultureInfo.InvariantCulture)}.gif?size=32"
                    : $"https://cdn.discordapp.com/emojis/{id.ToString(CultureInfo.InvariantCulture)}.png?size=32";
            IsAvailable = true;
            IsValid = true;
        }

        public readonly string Name { get; }
        public readonly string Unicode { get; }
        public readonly string Url { get; }
        public readonly bool IsAvailable { get; }
        public readonly bool IsValid { get; }
        public readonly double Opacity
            => this.IsAvailable ? 1.0 : 0.5;

        public readonly DiscordEmoji? DiscordEmoji =>
            _emoji ?? DiscordEmoji.FromUnicode(Unicode);

        public override string ToString()
        {
            return _emoji != null ? Formatter.Emoji(_emoji) : Unicode;
        }

        public override bool Equals(object obj)
        {
            return obj is EmojiViewModel model && model == this;
        }

        public override int GetHashCode()
        {
            int hashCode = -330246126;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Unicode);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Url);
            hashCode = hashCode * -1521134295 + IsAvailable.GetHashCode();
            hashCode = hashCode * -1521134295 + Opacity.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(EmojiViewModel lhs, EmojiViewModel rhs)
        {
            return lhs.Name == rhs.Name &&
                   lhs.Unicode == rhs.Unicode &&
                   lhs.Url == rhs.Url &&
                   lhs.IsAvailable == rhs.IsAvailable &&
                   lhs.Opacity == rhs.Opacity;
        }

        public static bool operator !=(EmojiViewModel lhs, EmojiViewModel rhs)
        {
            return !(lhs == rhs);
        }

        public static bool operator ==(EmojiViewModel lhs, DiscordEmoji rhs)
        {
            return lhs._emoji != null && lhs._emoji == rhs;
        }

        public static bool operator !=(EmojiViewModel lhs, DiscordEmoji rhs)
        {
            return !(lhs == rhs);
        }
    }
}

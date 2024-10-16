using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DSharpPlus.Entities;
using NeoSmart.Unicode;
using Unicord.Universal.Extensions;
using Unicord.Universal.Models.Emoji;

namespace Unicord.Universal.Misc
{
    public class EmojiHeader
    {
        public string Name { get; set; }
        public string IconCharacter { get; set; }
        public string IconUrl { get; set; }
    }

    public class EmojiGroup : IGrouping<EmojiHeader, EmojiViewModel>, IList<EmojiViewModel>
    {
        private List<EmojiViewModel> _emojis;

        public EmojiGroup(DiscordGuild guild, IEnumerable<DiscordEmoji> emojis)
        {
            Key = new EmojiHeader() { Name = guild.Name, IconUrl = guild.GetIconUrl(32) };
            _emojis = emojis.Select(e => new EmojiViewModel(e))
                   .OrderBy(e => e.Name)
                   .ToList();
        }

        public EmojiGroup(string category, IEnumerable<SingleEmoji> emojis)
        {
            var first = emojis.First();
            Key = new EmojiHeader() { Name = category, IconCharacter = first.Sequence.AsString };
            _emojis = emojis.Select(e => new EmojiViewModel(e)).ToList();
        }

        public EmojiViewModel this[int index] { get => ((IList<EmojiViewModel>)_emojis)[index]; set => ((IList<EmojiViewModel>)_emojis)[index] = value; }

        public EmojiHeader Key { get; }

        public int Count => ((ICollection<EmojiViewModel>)_emojis).Count;

        public bool IsReadOnly => ((ICollection<EmojiViewModel>)_emojis).IsReadOnly;

        public void Add(EmojiViewModel item)
        {
            ((ICollection<EmojiViewModel>)_emojis).Add(item);
        }

        public void Clear()
        {
            ((ICollection<EmojiViewModel>)_emojis).Clear();
        }

        public bool Contains(EmojiViewModel item)
        {
            return ((ICollection<EmojiViewModel>)_emojis).Contains(item);
        }

        public void CopyTo(EmojiViewModel[] array, int arrayIndex)
        {
            ((ICollection<EmojiViewModel>)_emojis).CopyTo(array, arrayIndex);
        }

        public IEnumerator<EmojiViewModel> GetEnumerator() => _emojis.GetEnumerator();

        public int IndexOf(EmojiViewModel item)
        {
            return ((IList<EmojiViewModel>)_emojis).IndexOf(item);
        }

        public void Insert(int index, EmojiViewModel item)
        {
            ((IList<EmojiViewModel>)_emojis).Insert(index, item);
        }

        public bool Remove(EmojiViewModel item)
        {
            return ((ICollection<EmojiViewModel>)_emojis).Remove(item);
        }

        public void RemoveAt(int index)
        {
            ((IList<EmojiViewModel>)_emojis).RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator() => _emojis.GetEnumerator();
    }
}

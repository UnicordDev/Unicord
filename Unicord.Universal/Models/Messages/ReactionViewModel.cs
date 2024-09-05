using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using DSharpPlus.Entities;
using Unicord.Universal.Commands.Messages;
using Unicord.Universal.Models.Emoji;

namespace Unicord.Universal.Models.Messages
{
    public class ReactionViewModel : ViewModelBase, IEquatable<DiscordReaction>
    {
        private DiscordReaction _reaction;

        public ReactionViewModel(DiscordReaction reaction, ICommand reactCommand)
            : base(null)
        {
            _reaction = reaction;
            ReactCommand = reactCommand;
        }
        public void Update(DiscordReaction reaction)
        {
            _reaction = reaction;
            InvokePropertyChanged(nameof(Count));
            InvokePropertyChanged(nameof(IsMe));
        }

        public EmojiViewModel Emoji => 
            new EmojiViewModel(_reaction.Emoji);
        public int Count =>
            _reaction.Count;
        public bool IsMe =>
            _reaction.IsMe;
        
        public ICommand ReactCommand { get; set; }

        public bool Equals(DiscordReaction other)
        {
            return _reaction.Emoji == other.Emoji;
        }
    }
}

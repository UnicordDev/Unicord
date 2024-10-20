using DSharpPlus.Entities;
using DSharpPlus.EventArgs;

namespace Unicord.Universal.Models.Guild
{
    internal class GuildListViewModel : GuildViewModel, IGuildListViewModel
    {
        private GuildListFolderViewModel _parent;
        private bool _isSelected;

        public GuildListViewModel(DiscordGuild guild, GuildListFolderViewModel parent = null) 
            : base(guild.Id)
        {
            _parent = parent;
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => OnPropertySet(ref _isSelected, value);
        }

        public int MentionCount
        {
            get
            {
                if (Muted)
                    return -1;

                var count = 0;
                foreach (var channel in AccessibleChannels)
                {
                    if (channel.Muted)
                        continue;

                    if (discord.ReadStates.TryGetValue(channel.Id, out var rs))
                        count += rs.MentionCount;
                }

                return count == 0 ? -1 : count;
            }
        }

        public bool TryGetModelForGuild(ulong guildId, out GuildListViewModel model)
        {
            if (Guild.Id == guildId)
            {
                model = this;
                return true;
            }

            model = null;
            return false;
        }

        protected override void OnReadStateUpdatedCore(ReadStateUpdateEventArgs e)
        {
            InvokePropertyChanged(nameof(MentionCount));
        }
    }
}

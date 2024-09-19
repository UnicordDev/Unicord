using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Toolkit.Mvvm.Messaging;
using Unicord.Universal.Models.Channels;

namespace Unicord.Universal.Models.Guild
{
    internal class GuildListViewModel : GuildViewModel, IGuildListViewModel
    {
        private GuildListFolderViewModel _parent;
        private bool _isSelected;

        public GuildListViewModel(DiscordGuild guild, GuildListFolderViewModel parent = null) :
            base(guild.Id)
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

        public bool TryGetModelForGuild(DiscordGuild guild, out GuildListViewModel model)
        {
            if (Guild == guild)
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

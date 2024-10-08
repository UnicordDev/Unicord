﻿using DSharpPlus.Entities;

namespace Unicord.Universal.Models.Guild
{
    internal interface IGuildListViewModel
    {
        string Name { get; }
        bool Unread { get; }
        int MentionCount { get; }
        bool TryGetModelForGuild(DiscordGuild guild, out GuildListViewModel model);
    }
}

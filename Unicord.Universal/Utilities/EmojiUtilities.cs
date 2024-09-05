using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using NeoSmart.Unicode;
using Unicord.Universal.Misc;
using Unicord.Universal.Models.Channels;
using Unicord.Universal.Models.Guild;

namespace Unicord.Universal.Utilities
{
    internal class EmojiUtilities
    {
        public static IList<EmojiGroup> GetEmoji(ChannelViewModel channel, string searchTerm = null)
        {
            var culture = CultureInfo.InvariantCulture.CompareInfo;
            var hasSearchTerm = !string.IsNullOrWhiteSpace(searchTerm);

            bool FilterEmoji(SingleEmoji e)
                => (!hasSearchTerm || culture.IndexOf(e.Name, searchTerm, CompareOptions.IgnoreCase) >= 0);

            var emoteList = GetAllowedEmotes(channel, searchTerm)
                .ToList();

            emoteList.AddRange(Emoji.All.Where(FilterEmoji)
                .GroupBy(e => e.Group)
                .Where(g => g.Any())
                .Select(g => new EmojiGroup(g.Key, g)));

            return emoteList;
        }

        public static IEnumerable<EmojiGroup> GetAllowedEmotes(ChannelViewModel channel, string searchTerm = null)
        {
            var hasNitro = App.Discord.CurrentUser.HasNitro();
            var culture = CultureInfo.InvariantCulture.CompareInfo;
            var hasSearchTerm = !string.IsNullOrWhiteSpace(searchTerm);

            bool FilterEmoji(DiscordEmoji e)
                => (hasNitro || !e.IsAnimated) &&
                e.IsAvailable
                && (!hasSearchTerm || culture.IndexOf(e.Name, searchTerm, CompareOptions.IgnoreCase) >= 0);

            if (hasNitro && (channel.Channel.IsPrivate || channel.Channel.CurrentPermissions.HasPermission(Permissions.UseExternalEmojis)))
            {
                // all availiable emoji 
                var guildOrder = GetOrderedGuildsList();

                return App.Discord.Guilds.Values
                    .OrderBy(g => guildOrder.IndexOf(g.Id))
                    .Select(g => new EmojiGroup(g, g.Emojis.Values.Where(FilterEmoji)))
                    .Where(g => g.Any());
            }
            else
            {
                // just this server's emoji
                if (channel.Guild == null)
                    return [];
                var group = new EmojiGroup(channel.Guild.Guild, channel.Guild.Guild.Emojis.Values.Where(FilterEmoji));

                return group.Count > 0 ? [group] : [];
            }
        }

        public static List<ulong> GetOrderedGuildsList()
        {
            var guilds = App.Discord.Guilds;
            var folders = App.Discord.UserSettings?.GuildFolders;
            var ids = new List<ulong>();
            foreach (var folder in (folders ?? []))
            {
                if (folder.Id == null || folder.Id == 0)
                {
                    foreach (var id in folder.GuildIds)
                    {
                        if (guilds.TryGetValue(id, out var server))
                        {
                            ids.Add(id);
                        }
                    }

                    continue;
                }
            }

            foreach (var guild in App.Discord.Guilds.Values)
            {
                if (!ids.Contains(guild.Id))
                    ids.Add(guild.Id);
            }

            return ids;
        }
    }
}

using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Unicord
{
    public static class SharedTools
    {
        public static bool WillShowToast(DiscordMessage message)
        {
            bool willNotify = false;

            if (message.MentionedUsers.Any(m => m?.Id == message.Discord.CurrentUser.Id))
            {
                willNotify = true;
            }

            if (message.Channel is DiscordDmChannel)
            {
                willNotify = true;
            }

            if (message.Channel.Guild != null)
            {
                var usr = message.Channel.Guild.CurrentMember;
                if (message.MentionedRoles?.Any(r => (usr.Roles.Contains(r))) == true)
                {
                    willNotify = true;
                }
            }

            if (message.Author.Id == message.Discord.CurrentUser.Id)
            {
                willNotify = false;
            }

            if ((message.Discord as DiscordClient)?.UserSettings.Status == "dnd")
            {
                willNotify = false;
            }

            return willNotify;
        }

        public static string GetMessageTitle(DiscordMessage message) => message.Channel.Guild != null ?
                               $"{(message.Author as DiscordMember)?.DisplayName ?? message.Author.Username} in {message.Channel.Guild.Name}" :
                               $"{message.Author.Username}";

        public static string GetMessageContent(DiscordMessage message)
        {
            string messageText = message.Content;

            foreach (DiscordUser user in message.MentionedUsers)
            {
                if (user != null)
                {
                    messageText = messageText
                        .Replace($"<@{user.Id}>", $"@{user.Username}")
                        .Replace($"<@!{user.Id}>", $"@{user.Username}");
                }
            }

            if (message.Channel.Guild != null)
            {
                foreach (DiscordChannel channel in message.MentionedChannels)
                {
                    messageText = messageText.Replace(channel.Mention, $"#{channel.Name}");
                }

                foreach (DiscordRole role in message.MentionedRoles)
                {
                    messageText = messageText.Replace(role.Mention, $"@{role.Name}");
                }
            }

            return messageText;
        }
    }
}

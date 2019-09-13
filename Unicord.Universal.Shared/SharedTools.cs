using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Net.Serialization;
using Newtonsoft.Json.Linq;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.Web.Http;

namespace Unicord.Universal.Shared
{
    public class SharedTools
    {
        private static readonly HttpClient _httpClient 
            = new HttpClient();

        public static async Task<DiscordMessage> SendFilesWithProgressAsync(DiscordChannel channel, string message, Dictionary<string, IInputStream> files, IProgress<double?> progress)
        {
            var httpRequestMessage
                = new HttpRequestMessage(HttpMethod.Post, new Uri("https://discordapp.com/api/v7" + string.Format("/channels/{0}/messages", channel.Id)));
            httpRequestMessage.Headers.Add("Authorization", Utilities.GetFormattedToken(channel.Discord));

            var cont = new HttpMultipartFormDataContent();

            if (!string.IsNullOrWhiteSpace(message))
            {
                cont.Add(new HttpStringContent(message), "content");
            }

            for (var i = 0; i < files.Count; i++)
            {
                var file = files.ElementAt(i);
                cont.Add(new HttpStreamContent(file.Value), $"file{i}", file.Key);
            }

            httpRequestMessage.Content = cont;

            var send = _httpClient.SendRequestAsync(httpRequestMessage);
            send.Progress += new AsyncOperationProgressHandler<HttpResponseMessage, HttpProgress>((o, e) =>
            {
                progress.Report((e.BytesSent / (double)e.TotalBytesToSend) * 100);
            });

            var resp = await send;
            var content = await resp.Content.ReadAsStringAsync();
            return JObject.Parse(content).ToDiscordObject<DiscordMessage>();
        }

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

            foreach (var user in message.MentionedUsers)
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
                foreach (var channel in message.MentionedChannels)
                {
                    messageText = messageText.Replace(channel.Mention, $"#{channel.Name}");
                }

                foreach (var role in message.MentionedRoles)
                {
                    messageText = messageText.Replace(role.Mention, $"@{role.Name}");
                }
            }

            return messageText;
        }
    }
}

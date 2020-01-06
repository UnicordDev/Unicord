using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.Net.Serialization;
using Newtonsoft.Json;

namespace DSharpPlus.Entities
{
    /// <inheritdoc />
    /// <summary>
    /// Represents a direct message channel.
    /// </summary>
    public class DiscordDmChannel : DiscordChannel
    {
        private string _iconHash;
        private DiscordCall _ongoingCall;

        /// <summary>
        /// Gets the recipients of this direct message.
        /// </summary>
        public IReadOnlyDictionary<ulong, DiscordUser> Recipients
            => new ReadOnlyConcurrentDictionary<ulong, DiscordUser>(_recipients);

        [JsonProperty("recipient", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(SnowflakeArrayAsDictionaryJsonConverter))]
        internal ConcurrentDictionary<ulong, DiscordUser> _recipients;

        /// <summary>
        /// Gets the hash of this channel's icon.
        /// </summary>
        [JsonProperty("icon", NullValueHandling = NullValueHandling.Ignore)]
        public string IconHash { get => _iconHash; internal set => OnPropertySet(ref _iconHash, value); }

        /// <summary>
        /// Gets the URL of this channel's icon.
        /// </summary>
        [JsonIgnore]
        public string IconUrl
            => !string.IsNullOrWhiteSpace(IconHash) ? $"https://cdn.discordapp.com/channel-icons/{Id.ToString(CultureInfo.InvariantCulture)}/{IconHash}.png" : Recipient?.NonAnimatedAvatarUrl;

        [JsonIgnore]
        public DiscordUser Recipient => Type == ChannelType.Private ? _recipients.Values.ElementAt(0) : null;

        [JsonIgnore]
        public DiscordCall OngoingCall { get => _ongoingCall; internal set => OnPropertySet(ref _ongoingCall, value); }

        /// <summary>
        /// Only use for Group DMs! Whitelised bots only. Requires user's oauth2 access token
        /// </summary>
        public Task AddDmRecipientAsync(ulong user_id, string accesstoken, string nickname)
            => Discord.ApiClient.GroupDmAddRecipientAsync(Id, user_id, accesstoken, nickname);

        /// <summary>
        /// Only use for Group DMs!
        /// </summary>
        public Task RemoveDmRecipientAsync(ulong user_id, string accesstoken)
            => Discord.ApiClient.GroupDmRemoveRecipientAsync(Id, user_id);
    }
}

using Newtonsoft.Json;

namespace DSharpPlus.VoiceNext.Entities
{
    internal sealed class VoiceStateUpdatePayload
    {
        [JsonProperty("guild_id")]
        public ulong? GuildId { get; set; }

        [JsonProperty("channel_id")]
        public ulong? ChannelId { get; set; }

        [JsonProperty("user_id", NullValueHandling = NullValueHandling.Ignore)]
        public ulong? UserId { get; set; }

        [JsonProperty("session_id", NullValueHandling = NullValueHandling.Ignore)]
        public string SessionId { get; set; }

        [JsonProperty("self_deaf", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Deafened { get; set; }

        [JsonProperty("self_mute", NullValueHandling = NullValueHandling.Ignore)]
        public bool? Muted { get; set; }
    }
}

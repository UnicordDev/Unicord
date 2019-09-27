using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.Net.Serialization;
using Newtonsoft.Json;

namespace DSharpPlus.Entities
{
    public class DiscordCall : PropertyChangedBase
    {
        internal DiscordClient Discord { get; set; }

        [JsonProperty("region")]
        internal string _voiceRegion;
        [JsonProperty("channel_id")]
        internal ulong _channelId;

        /// <summary>
        /// Gets a collection of all the voice states for this call
        /// </summary>
        [JsonIgnore]
        public IReadOnlyDictionary<ulong, DiscordVoiceState> VoiceStates
            => new ReadOnlyConcurrentDictionary<ulong, DiscordVoiceState>(_voiceStates);

        [JsonProperty("voice_states", NullValueHandling = NullValueHandling.Ignore)]
        [JsonConverter(typeof(SnowflakeArrayAsDictionaryJsonConverter))]
        internal ConcurrentDictionary<ulong, DiscordVoiceState> _voiceStates
            = new ConcurrentDictionary<ulong, DiscordVoiceState>();

        [JsonProperty("ringing")]
        public List<ulong> Ringing { get; internal set; }

        [JsonProperty("message_id")]
        public ulong MessageId { get; set; }

        [JsonIgnore]
        public DiscordVoiceRegion VoiceRegion => Discord.VoiceRegions.TryGetValue(_voiceRegion, out var value) ? value : null;

        [JsonIgnore]
        public DiscordDmChannel Channel => Discord.PrivateChannels.TryGetValue(_channelId, out var value) ? value : null;

        public override string ToString()
        {
            return $"Call with {Channel.Recipient.DisplayName} ({Channel.Id}). {Ringing.Count} ringing. {_voiceStates.Count} states.";
        }
    }
}

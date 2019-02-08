using Newtonsoft.Json;

namespace DSharpPlus.VoiceNext.VoiceEntities
{
    internal sealed class VoiceSelectProtocolPayload
    {
        [JsonProperty("protocol")]
        public string Protocol { get; set; }

        [JsonProperty("data")]
        public VoiceSelectProtocolPayloadData Data { get; set; }

        [JsonProperty("address")]
        public string Address { get; set; }

        [JsonProperty("port")]
        public ushort Port { get; set; }

        [JsonProperty("mode")]
        public string Mode { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Unicord.Universal.Voice.Transport
{
    internal sealed class VoiceDispatch
    {
        [JsonProperty("op")]
        public int OpCode { get; set; }

        [JsonProperty("d")]
        public object Payload { get; set; }

        [JsonProperty("s", NullValueHandling = NullValueHandling.Ignore)]
        public int? Sequence { get; set; }

        [JsonProperty("t", NullValueHandling = NullValueHandling.Ignore)]
        public string EventName { get; set; }
    }
}

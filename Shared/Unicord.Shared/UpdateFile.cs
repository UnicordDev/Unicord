using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Unicord.Shared
{
    public class UpdateFile
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("hash", NullValueHandling = NullValueHandling.Ignore)]
        public byte[] Hash { get; set; }

        [JsonProperty("included_version", NullValueHandling = NullValueHandling.Ignore)]
        internal string _includedVersion;

        [JsonIgnore]
        public Version IncludedVersion
        {
            get => _includedVersion != null ? Version.Parse(_includedVersion) : null;
            set => _includedVersion = value.ToString();
        }
    }
}

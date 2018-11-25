using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Unicord.Shared
{
    public class UpdateDetails
    {
        [JsonProperty("version")]
        internal string _version;

        [JsonIgnore]
        public Version Version
        {
            get => Version.Parse(_version);
            set => _version = value.ToString();
        }

        [JsonProperty("version_details")]
        public string VersionDetails { get; set; }

        [JsonProperty("main_exe")]
        public string MainExecutable { get; set; }

        [JsonProperty("platform")]
        public string Platform { get; set; }

        [JsonProperty("released_at")]
        public DateTimeOffset ReleaseDate { get; set; }

        [JsonProperty("files")]
        public List<UpdateFile> Files { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace mcversort {
    public class MinecraftVersion {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("time")]
        public DateTime Time { get; set; }

        [JsonPropertyName("releaseTime")]
        public DateTime ReleaseTime { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("inheritsFrom")]
        public string InheritsFrom { get; set; }

        [JsonIgnore]
        public MinecraftVersion InheritingVersion { get; set; }

        [JsonIgnore]
        public VersionType VersionType { get; set; }
    }
}

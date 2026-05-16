using System;
using System.Collections.Generic;

namespace ValheimSessionChronicle.Models
{
    public sealed class SessionEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");
        public DateTime TimestampUtc { get; set; }
        public double SessionSeconds { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Actor { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string Biome { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Importance { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
}

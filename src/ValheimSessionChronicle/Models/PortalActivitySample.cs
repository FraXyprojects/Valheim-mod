using System;

namespace ValheimSessionChronicle.Models
{
    public sealed class PortalActivitySample
    {
        public DateTime TimestampUtc { get; set; }
        public string PortalName { get; set; } = string.Empty;
        public string Biome { get; set; } = string.Empty;
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }
}

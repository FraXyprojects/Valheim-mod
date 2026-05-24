using System;

namespace ValheimSessionChronicle.WorldMemory
{
    public sealed class WorldProgressionObservation
    {
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
        public string SessionId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Biome { get; set; } = string.Empty;
    }
}

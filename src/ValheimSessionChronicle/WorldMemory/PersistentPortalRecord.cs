using System;

namespace ValheimSessionChronicle.WorldMemory
{
    public sealed class PersistentPortalRecord
    {
        public string PortalName { get; set; } = string.Empty;
        public DateTime FirstSeenUtc { get; set; } = DateTime.UtcNow;
        public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;
        public int UsageCount { get; set; }
        public string LinkedBiome { get; set; } = string.Empty;
        public float ApproximateX { get; set; }
        public float ApproximateY { get; set; }
        public float ApproximateZ { get; set; }
    }
}

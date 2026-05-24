using System;

namespace ValheimSessionChronicle.WorldMemory
{
    public sealed class PersistentCampTierObservation
    {
        public DateTime TimestampUtc { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public int Tier { get; set; }
        public string TierName { get; set; } = string.Empty;
        public int StructureCount { get; set; }
    }
}

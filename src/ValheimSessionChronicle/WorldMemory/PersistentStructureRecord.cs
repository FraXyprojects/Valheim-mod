using System;

namespace ValheimSessionChronicle.WorldMemory
{
    public sealed class PersistentStructureRecord
    {
        public string StructureName { get; set; } = string.Empty;
        public string StructureType { get; set; } = string.Empty;
        public string Biome { get; set; } = string.Empty;
        public DateTime FirstSeenUtc { get; set; } = DateTime.UtcNow;
        public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;
        public string FirstSeenSessionId { get; set; } = string.Empty;
        public int ObservationCount { get; set; }
        public float ApproximateX { get; set; }
        public float ApproximateY { get; set; }
        public float ApproximateZ { get; set; }
    }
}

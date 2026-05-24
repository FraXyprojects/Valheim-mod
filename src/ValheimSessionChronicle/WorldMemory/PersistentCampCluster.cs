using System;
using System.Collections.Generic;

namespace ValheimSessionChronicle.WorldMemory
{
    public sealed class PersistentCampCluster
    {
        public string ClusterId { get; set; } = Guid.NewGuid().ToString("N");
        public float CenterX { get; set; }
        public float CenterY { get; set; }
        public float CenterZ { get; set; }
        public string Biome { get; set; } = string.Empty;
        public DateTime FirstSeenUtc { get; set; } = DateTime.UtcNow;
        public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;
        public string LastExpandedSessionId { get; set; } = string.Empty;
        public int CurrentTier { get; set; }
        public string CurrentTierName { get; set; } = string.Empty;
        public int StructureCount { get; set; }
        public int BedCount { get; set; }
        public int DefenseCount { get; set; }
        public int StorageCount { get; set; }
        public bool HasFire { get; set; }
        public bool HasWorkbench { get; set; }
        public bool HasForge { get; set; }
        public bool HasPortal { get; set; }
        public bool HasAdvancedStation { get; set; }
        public List<string> StationTypes { get; set; } = new List<string>();
        public List<string> PortalNames { get; set; } = new List<string>();
        public List<PersistentCampTierObservation> TierHistory { get; set; } = new List<PersistentCampTierObservation>();
    }
}

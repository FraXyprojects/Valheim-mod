using System;
using System.Collections.Generic;

namespace ValheimSessionChronicle.WorldMemory
{
    public sealed class WorldMemoryData
    {
        public string SchemaVersion { get; set; } = "1.0";
        public string WorldKey { get; set; } = string.Empty;
        public string WorldName { get; set; } = string.Empty;
        public string ServerName { get; set; } = string.Empty;
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
        public int SessionCount { get; set; }
        public string LastSessionId { get; set; } = string.Empty;
        public List<PersistentCampCluster> Camps { get; set; } = new List<PersistentCampCluster>();
        public List<PersistentPortalRecord> Portals { get; set; } = new List<PersistentPortalRecord>();
        public List<PersistentStructureRecord> ImportantStructures { get; set; } = new List<PersistentStructureRecord>();
        public List<string> DiscoveredBiomes { get; set; } = new List<string>();
        public List<string> ImportantItems { get; set; } = new List<string>();
        public List<string> BossesDefeated { get; set; } = new List<string>();
        public List<WorldProgressionObservation> ProgressionObservations { get; set; } = new List<WorldProgressionObservation>();
    }
}

using System.Collections.Generic;

namespace ValheimSessionChronicle.WorldMemory
{
    public sealed class WorldMemoryUpdateResult
    {
        public List<PersistentCampChange> CampChanges { get; set; } = new List<PersistentCampChange>();
        public List<PersistentPortalRecord> NewPortals { get; set; } = new List<PersistentPortalRecord>();
        public List<PersistentStructureRecord> NewImportantStructures { get; set; } = new List<PersistentStructureRecord>();
        public HashSet<string> PreviouslyKnownBiomes { get; set; } = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        public HashSet<string> PreviouslyKnownImportantItems { get; set; } = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        public HashSet<string> PreviouslyKnownBosses { get; set; } = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
        public HashSet<string> PreviouslyKnownImportantStructures { get; set; } = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
    }
}

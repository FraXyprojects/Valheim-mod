namespace ValheimSessionChronicle.WorldMemory
{
    public sealed class PersistentCampChange
    {
        public string ClusterId { get; set; } = string.Empty;
        public string Biome { get; set; } = string.Empty;
        public string PreviousTierName { get; set; } = string.Empty;
        public string NewTierName { get; set; } = string.Empty;
        public int PreviousTier { get; set; }
        public int NewTier { get; set; }
        public int AddedStructures { get; set; }
        public bool IsNewCamp { get; set; }
        public bool IsTierUpgrade => NewTier > PreviousTier;
        public bool AddedForge { get; set; }
        public bool AddedPortal { get; set; }
        public bool AddedDefenses { get; set; }
        public bool AddedStorage { get; set; }
        public bool AddedAdvancedStation { get; set; }
    }
}

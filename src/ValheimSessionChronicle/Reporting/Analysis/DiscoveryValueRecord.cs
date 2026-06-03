namespace ValheimSessionChronicle.Reporting.Analysis
{
    public sealed class DiscoveryValueRecord
    {
        public string Name { get; set; } = string.Empty;
        public string Kind { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public bool WasKnownBefore { get; set; }
        public DiscoveryValueTier Tier { get; set; }
        public string Summary { get; set; } = string.Empty;
    }
}

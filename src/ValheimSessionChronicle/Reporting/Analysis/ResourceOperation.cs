namespace ValheimSessionChronicle.Reporting.Analysis
{
    public sealed class ResourceOperation
    {
        public string ResourceName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string OperationType { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public ProgressionStage RelatedStage { get; set; }
        public string Summary { get; set; } = string.Empty;
    }
}

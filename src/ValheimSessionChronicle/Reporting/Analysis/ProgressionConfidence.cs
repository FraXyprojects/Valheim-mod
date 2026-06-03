namespace ValheimSessionChronicle.Reporting.Analysis
{
    public sealed class ProgressionConfidence
    {
        public ProgressionStage Stage { get; set; }
        public string Label { get; set; } = string.Empty;
        public double Score { get; set; }
        public int Percentage { get; set; }
    }
}

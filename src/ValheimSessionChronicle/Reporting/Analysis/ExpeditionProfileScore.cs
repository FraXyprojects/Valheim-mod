namespace ValheimSessionChronicle.Reporting.Analysis
{
    public sealed class ExpeditionProfileScore
    {
        public string Label { get; set; } = string.Empty;
        public string FileToken { get; set; } = string.Empty;
        public double RawScore { get; set; }
        public int Percentage { get; set; }
    }
}

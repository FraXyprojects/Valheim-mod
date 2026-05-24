namespace ValheimSessionChronicle.Reporting.Analysis
{
    public sealed class SurvivalSummary
    {
        public bool HasHealthData { get; set; }
        public float LowestHealth { get; set; }
        public float LowestHealthPercent { get; set; }
        public int NearDeathMoments { get; set; }
        public int IncomingHits { get; set; }
        public float TotalIncomingDamage { get; set; }
        public int Deaths { get; set; }
    }
}

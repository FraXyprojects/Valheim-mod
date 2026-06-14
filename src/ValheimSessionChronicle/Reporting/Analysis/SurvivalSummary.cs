using System.Collections.Generic;

namespace ValheimSessionChronicle.Reporting.Analysis
{
    public sealed class SurvivalSummary
    {
        public bool HasHealthData { get; set; }
        public float LowestHealth { get; set; }
        public float LowestHealthPercent { get; set; }
        public int NearDeathMoments { get; set; }
        public int NearDeathEscapes { get; set; }
        public int HeroicEscapes { get; set; }
        public int LastStandMoments { get; set; }
        public int IncomingHits { get; set; }
        public int MaxConsecutiveHits { get; set; }
        public float TotalIncomingDamage { get; set; }
        public float LargestSingleHit { get; set; }
        public float LargestSingleHitPercent { get; set; }
        public double CombatStressScore { get; set; }
        public CombatIntensityTier StressTier { get; set; }
        public int Deaths { get; set; }
        public List<CombatWindow> CombatWindows { get; set; } = new List<CombatWindow>();
        public CombatWindow FiercestWindow { get; set; }
    }
}

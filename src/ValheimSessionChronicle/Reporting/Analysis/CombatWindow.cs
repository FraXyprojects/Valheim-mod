using System;

namespace ValheimSessionChronicle.Reporting.Analysis
{
    public sealed class CombatWindow
    {
        public DateTime StartUtc { get; set; }
        public DateTime EndUtc { get; set; }
        public int Kills { get; set; }
        public int IncomingHits { get; set; }
        public float DamageTaken { get; set; }
        public float LowestHealthPercent { get; set; }
        public bool NearDeath { get; set; }
        public bool HeroicEscape { get; set; }
        public bool LastStand { get; set; }
        public double StressScore { get; set; }
        public string DominantBiome { get; set; } = string.Empty;
    }
}

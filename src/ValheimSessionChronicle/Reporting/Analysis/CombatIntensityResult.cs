using System.Collections.Generic;

namespace ValheimSessionChronicle.Reporting.Analysis
{
    public sealed class CombatIntensityResult
    {
        public CombatIntensityTier Tier { get; set; }
        public double Score { get; set; }
        public int TotalKills { get; set; }
        public int EliteKills { get; set; }
        public int IncomingHits { get; set; }
        public int NearDeathMoments { get; set; }
        public int LongestCombatChainSeconds { get; set; }
        public string DominantCombatBiome { get; set; } = string.Empty;
        public Dictionary<string, int> KillsByBiome { get; set; } = new Dictionary<string, int>();
    }
}

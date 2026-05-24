using System.Linq;
using ValheimSessionChronicle.Models;

namespace ValheimSessionChronicle.Reporting.Analysis
{
    public sealed class SurvivalAnalyzer
    {
        public SurvivalSummary Analyze(SessionData session)
        {
            var incoming = session.CombatSamples
                .Where(sample => sample.IsIncomingDamage)
                .ToList();

            SurvivalSummary summary = new SurvivalSummary
            {
                IncomingHits = incoming.Count,
                TotalIncomingDamage = incoming.Sum(sample => sample.IncomingDamage),
                NearDeathMoments = incoming.Count(sample => sample.IsNearDeath),
                Deaths = session.PlayerStats.Values.Sum(stats => stats.Deaths)
            };

            var healthSamples = incoming.Where(sample => sample.HealthAfterHit > 0f).ToList();
            summary.HasHealthData = healthSamples.Count > 0;
            if (summary.HasHealthData)
            {
                summary.LowestHealth = healthSamples.Min(sample => sample.HealthAfterHit);
                float maxHealth = healthSamples
                    .Where(sample => sample.MaxHealth > 0f)
                    .OrderBy(sample => sample.HealthAfterHit)
                    .Select(sample => sample.MaxHealth)
                    .FirstOrDefault();
                summary.LowestHealthPercent = maxHealth > 0f ? summary.LowestHealth / maxHealth : 0f;
            }

            return summary;
        }
    }
}

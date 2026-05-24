using System;
using System.Collections.Generic;
using System.Linq;
using ValheimSessionChronicle.Models;
using ValheimSessionChronicle.Utility;

namespace ValheimSessionChronicle.Reporting.Analysis
{
    public sealed class CombatIntensityAnalyzer
    {
        public CombatIntensityResult Analyze(SessionData session)
        {
            List<CombatActivitySample> samples = session.CombatSamples
                .OrderBy(sample => sample.TimestampUtc)
                .ToList();

            CombatIntensityResult result = new CombatIntensityResult
            {
                TotalKills = samples.Count(sample => sample.IsKill && !sample.IsBoss),
                EliteKills = samples.Count(sample => sample.IsKill && sample.IsEliteEnemy),
                IncomingHits = samples.Count(sample => sample.IsIncomingDamage),
                NearDeathMoments = samples.Count(sample => sample.IsNearDeath)
            };

            result.KillsByBiome = samples
                .Where(sample => sample.IsKill && ChronicleFilters.IsValidBiome(sample.Biome))
                .GroupBy(sample => sample.Biome, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);

            result.DominantCombatBiome = result.KillsByBiome
                .OrderByDescending(pair => pair.Value)
                .Select(pair => pair.Key)
                .FirstOrDefault() ?? string.Empty;

            result.LongestCombatChainSeconds = EstimateLongestCombatChain(samples);
            double burstScore = Math.Max(ScoreWindow(samples, TimeSpan.FromSeconds(30)), ScoreWindow(samples, TimeSpan.FromSeconds(90)));
            double pressureScore =
                result.TotalKills * 0.8 +
                result.EliteKills * 3.0 +
                result.IncomingHits * 1.2 +
                result.NearDeathMoments * 8.0 +
                session.PlayerStats.Values.Sum(stats => stats.DangerousEncounters) * 2.0 +
                result.LongestCombatChainSeconds / 30.0;

            if (string.Equals(result.DominantCombatBiome, "Swamp", StringComparison.OrdinalIgnoreCase))
            {
                pressureScore += result.KillsByBiome[result.DominantCombatBiome] * 0.8;
            }

            result.Score = Math.Max(burstScore, 0) + pressureScore;
            result.Tier = ToTier(result.Score);
            return result;
        }

        private static double ScoreWindow(IReadOnlyList<CombatActivitySample> samples, TimeSpan window)
        {
            double best = 0;
            for (int index = 0; index < samples.Count; index++)
            {
                DateTime start = samples[index].TimestampUtc;
                DateTime end = start + window;
                List<CombatActivitySample> slice = samples
                    .Where(sample => sample.TimestampUtc >= start && sample.TimestampUtc <= end)
                    .ToList();

                int kills = slice.Count(sample => sample.IsKill && !sample.IsBoss);
                int eliteKills = slice.Count(sample => sample.IsKill && sample.IsEliteEnemy);
                int diversity = slice.Where(sample => sample.IsKill).Select(sample => sample.EnemyName).Distinct(StringComparer.OrdinalIgnoreCase).Count();
                int incoming = slice.Count(sample => sample.IsIncomingDamage);
                int nearDeath = slice.Count(sample => sample.IsNearDeath);
                int deaths = slice.Count(sample => sample.CausedDeath);

                double score = kills * (window.TotalSeconds <= 30 ? 5.0 : 2.5) +
                               eliteKills * 7.0 +
                               diversity * 2.0 +
                               incoming * 1.5 +
                               nearDeath * 10.0 +
                               deaths * 12.0;
                best = Math.Max(best, score);
            }

            return best;
        }

        private static int EstimateLongestCombatChain(IReadOnlyList<CombatActivitySample> samples)
        {
            if (samples.Count == 0)
            {
                return 0;
            }

            int best = 0;
            DateTime chainStart = samples[0].TimestampUtc;
            DateTime previous = chainStart;

            for (int index = 1; index < samples.Count; index++)
            {
                DateTime current = samples[index].TimestampUtc;
                if ((current - previous).TotalSeconds > 45)
                {
                    best = Math.Max(best, (int)(previous - chainStart).TotalSeconds);
                    chainStart = current;
                }

                previous = current;
            }

            return Math.Max(best, (int)(previous - chainStart).TotalSeconds);
        }

        private static CombatIntensityTier ToTier(double score)
        {
            if (score >= 85)
            {
                return CombatIntensityTier.Extreme;
            }

            if (score >= 45)
            {
                return CombatIntensityTier.High;
            }

            if (score >= 20)
            {
                return CombatIntensityTier.Medium;
            }

            return CombatIntensityTier.Low;
        }
    }
}

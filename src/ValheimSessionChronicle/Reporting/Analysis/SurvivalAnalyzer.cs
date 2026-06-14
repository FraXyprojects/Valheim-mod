using System;
using System.Collections.Generic;
using System.Linq;
using ValheimSessionChronicle.Models;
using ValheimSessionChronicle.Utility;

namespace ValheimSessionChronicle.Reporting.Analysis
{
    public sealed class SurvivalAnalyzer
    {
        private static readonly TimeSpan CombatWindowGap = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan NearDeathSurvivalTime = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan HeroicEscapeSurvivalTime = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan LastStandWindow = TimeSpan.FromSeconds(30);

        public SurvivalSummary Analyze(SessionData session)
        {
            List<CombatActivitySample> samples = session.CombatSamples
                .OrderBy(sample => sample.TimestampUtc)
                .ToList();
            List<CombatActivitySample> incoming = samples
                .Where(sample => sample.IsIncomingDamage)
                .ToList();

            SurvivalSummary summary = new SurvivalSummary
            {
                IncomingHits = incoming.Count,
                TotalIncomingDamage = incoming.Sum(sample => sample.IncomingDamage),
                Deaths = session.PlayerStats.Values.Sum(stats => stats.Deaths),
                MaxConsecutiveHits = CalculateMaxConsecutiveHits(incoming)
            };

            AddHealthMetrics(summary, incoming);
            summary.NearDeathEscapes = CountNearDeathEscapes(session, samples, incoming);
            summary.HeroicEscapes = CountHeroicEscapes(session, samples, incoming);
            summary.LastStandMoments = CountLastStandMoments(samples, incoming);
            summary.NearDeathMoments = summary.NearDeathEscapes;
            summary.CombatWindows = BuildCombatWindows(samples);
            summary.FiercestWindow = summary.CombatWindows
                .OrderByDescending(window => window.StressScore)
                .FirstOrDefault();
            summary.CombatStressScore = CalculateStressScore(session, summary);
            summary.StressTier = ToTier(summary.CombatStressScore);

            return summary;
        }

        private static void AddHealthMetrics(SurvivalSummary summary, IReadOnlyList<CombatActivitySample> incoming)
        {
            List<CombatActivitySample> healthSamples = incoming
                .Where(sample => sample.HealthAfterHit > 0f && sample.MaxHealth > 0f)
                .ToList();

            summary.HasHealthData = healthSamples.Count > 0;
            if (!summary.HasHealthData)
            {
                return;
            }

            CombatActivitySample lowest = healthSamples
                .OrderBy(sample => sample.HealthPercent > 0f ? sample.HealthPercent : sample.HealthAfterHit / sample.MaxHealth)
                .First();
            summary.LowestHealth = lowest.HealthAfterHit;
            summary.LowestHealthPercent = lowest.HealthPercent > 0f
                ? lowest.HealthPercent
                : lowest.HealthAfterHit / lowest.MaxHealth;

            CombatActivitySample largest = healthSamples
                .OrderByDescending(sample => sample.DamagePercentOfMaxHealth > 0f
                    ? sample.DamagePercentOfMaxHealth
                    : sample.IncomingDamage / sample.MaxHealth)
                .First();
            summary.LargestSingleHit = largest.IncomingDamage;
            summary.LargestSingleHitPercent = largest.DamagePercentOfMaxHealth > 0f
                ? largest.DamagePercentOfMaxHealth
                : largest.IncomingDamage / largest.MaxHealth;
        }

        private static int CountNearDeathEscapes(
            SessionData session,
            IReadOnlyList<CombatActivitySample> samples,
            IReadOnlyList<CombatActivitySample> incoming)
        {
            return CountDistinctCriticalMoments(incoming
                .Where(sample => IsCriticalHealth(sample, 0.10f) &&
                                 SurvivedFor(session, samples, sample, NearDeathSurvivalTime)));
        }

        private static int CountHeroicEscapes(
            SessionData session,
            IReadOnlyList<CombatActivitySample> samples,
            IReadOnlyList<CombatActivitySample> incoming)
        {
            return CountDistinctCriticalMoments(incoming
                .Where(sample => IsCriticalHealth(sample, 0.05f) &&
                                 SurvivedFor(session, samples, sample, HeroicEscapeSurvivalTime) &&
                                 ContinuedCombat(samples, sample, HeroicEscapeSurvivalTime)));
        }

        private static int CountLastStandMoments(
            IReadOnlyList<CombatActivitySample> samples,
            IReadOnlyList<CombatActivitySample> incoming)
        {
            return CountDistinctCriticalMoments(incoming
                .Where(sample => IsCriticalHealth(sample, 0.10f) &&
                                 CountKillsAfter(samples, sample, LastStandWindow) >= 3));
        }

        private static int CountDistinctCriticalMoments(IEnumerable<CombatActivitySample> samples)
        {
            int count = 0;
            DateTime lastCounted = DateTime.MinValue;
            foreach (CombatActivitySample sample in samples.OrderBy(sample => sample.TimestampUtc))
            {
                if (lastCounted != DateTime.MinValue && sample.TimestampUtc - lastCounted <= CombatWindowGap)
                {
                    continue;
                }

                count++;
                lastCounted = sample.TimestampUtc;
            }

            return count;
        }

        private static bool SurvivedFor(
            SessionData session,
            IReadOnlyList<CombatActivitySample> samples,
            CombatActivitySample sample,
            TimeSpan duration)
        {
            DateTime end = sample.TimestampUtc + duration;
            bool diedInsideWindow = samples.Any(entry =>
                entry.CausedDeath &&
                string.Equals(entry.Actor, sample.Actor, StringComparison.OrdinalIgnoreCase) &&
                entry.TimestampUtc > sample.TimestampUtc &&
                entry.TimestampUtc <= end);

            if (diedInsideWindow)
            {
                return false;
            }

            DateTime sessionEnd = session.EndTimeUtc == default(DateTime) ? DateTime.UtcNow : session.EndTimeUtc;
            return sessionEnd >= end || samples.Any(entry => entry.TimestampUtc >= end);
        }

        private static bool ContinuedCombat(
            IReadOnlyList<CombatActivitySample> samples,
            CombatActivitySample sample,
            TimeSpan duration)
        {
            DateTime end = sample.TimestampUtc + duration;
            return samples.Any(entry =>
                string.Equals(entry.Actor, sample.Actor, StringComparison.OrdinalIgnoreCase) &&
                entry.TimestampUtc > sample.TimestampUtc &&
                entry.TimestampUtc <= end &&
                (entry.IsKill || entry.IsIncomingDamage));
        }

        private static int CountKillsAfter(
            IReadOnlyList<CombatActivitySample> samples,
            CombatActivitySample sample,
            TimeSpan duration)
        {
            DateTime end = sample.TimestampUtc + duration;
            return samples.Count(entry =>
                string.Equals(entry.Actor, sample.Actor, StringComparison.OrdinalIgnoreCase) &&
                entry.TimestampUtc > sample.TimestampUtc &&
                entry.TimestampUtc <= end &&
                entry.IsKill &&
                !entry.IsBoss);
        }

        private static int CalculateMaxConsecutiveHits(IReadOnlyList<CombatActivitySample> incoming)
        {
            int best = 0;
            for (int index = 0; index < incoming.Count; index++)
            {
                DateTime start = incoming[index].TimestampUtc;
                DateTime end = start + TimeSpan.FromSeconds(12);
                int count = incoming.Count(sample => sample.TimestampUtc >= start && sample.TimestampUtc <= end);
                best = Math.Max(best, count);
            }

            return best;
        }

        private static List<CombatWindow> BuildCombatWindows(IReadOnlyList<CombatActivitySample> samples)
        {
            List<CombatActivitySample> relevant = samples
                .Where(sample => sample.IsKill || sample.IsIncomingDamage || sample.CausedDeath)
                .OrderBy(sample => sample.TimestampUtc)
                .ToList();
            List<CombatWindow> windows = new List<CombatWindow>();
            if (relevant.Count == 0)
            {
                return windows;
            }

            List<CombatActivitySample> current = new List<CombatActivitySample> { relevant[0] };
            for (int index = 1; index < relevant.Count; index++)
            {
                CombatActivitySample previous = relevant[index - 1];
                CombatActivitySample next = relevant[index];
                if (next.TimestampUtc - previous.TimestampUtc > CombatWindowGap)
                {
                    windows.Add(BuildWindow(current));
                    current = new List<CombatActivitySample>();
                }

                current.Add(next);
            }

            windows.Add(BuildWindow(current));
            return windows
                .OrderByDescending(window => window.StressScore)
                .Take(8)
                .OrderBy(window => window.StartUtc)
                .ToList();
        }

        private static CombatWindow BuildWindow(IReadOnlyList<CombatActivitySample> samples)
        {
            List<CombatActivitySample> incoming = samples.Where(sample => sample.IsIncomingDamage).ToList();
            List<CombatActivitySample> healthSamples = incoming
                .Where(sample => sample.HealthPercent > 0f)
                .ToList();
            int kills = samples.Count(sample => sample.IsKill && !sample.IsBoss);
            int nearDeath = incoming.Count(sample => IsCriticalHealth(sample, 0.10f));
            float damage = incoming.Sum(sample => sample.IncomingDamage);
            float lowestHealthPercent = healthSamples.Count > 0 ? healthSamples.Min(sample => sample.HealthPercent) : 1f;
            string dominantBiome = samples
                .Where(sample => ChronicleFilters.IsValidBiome(sample.Biome))
                .GroupBy(sample => sample.Biome, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(group => group.Count())
                .Select(group => group.Key)
                .FirstOrDefault() ?? string.Empty;

            double stress = kills * 3.0 +
                            incoming.Count * 2.0 +
                            damage * 0.08 +
                            nearDeath * 18.0 +
                            samples.Count(sample => sample.IsEliteEnemy) * 5.0 +
                            samples.Count(sample => sample.CausedDeath) * 20.0;

            return new CombatWindow
            {
                StartUtc = samples.First().TimestampUtc,
                EndUtc = samples.Last().TimestampUtc,
                Kills = kills,
                IncomingHits = incoming.Count,
                DamageTaken = damage,
                LowestHealthPercent = lowestHealthPercent,
                NearDeath = nearDeath > 0,
                LastStand = nearDeath > 0 && kills >= 3,
                HeroicEscape = healthSamples.Any(sample => sample.HealthPercent <= 0.05f) && kills > 0,
                StressScore = stress,
                DominantBiome = dominantBiome
            };
        }

        private static double CalculateStressScore(SessionData session, SurvivalSummary summary)
        {
            double windowStress = summary.FiercestWindow?.StressScore ?? 0;
            int dangerous = session.PlayerStats.Values.Sum(stats => stats.DangerousEncounters);
            double healthPressure = summary.HasHealthData
                ? Math.Max(0, 1.0 - summary.LowestHealthPercent) * 30.0
                : 0;

            return windowStress +
                   summary.IncomingHits * 1.2 +
                   summary.TotalIncomingDamage * 0.03 +
                   summary.NearDeathEscapes * 18.0 +
                   summary.HeroicEscapes * 25.0 +
                   summary.LastStandMoments * 22.0 +
                   summary.MaxConsecutiveHits * 4.0 +
                   dangerous * 2.5 +
                   healthPressure;
        }

        private static CombatIntensityTier ToTier(double score)
        {
            if (score >= 95)
            {
                return CombatIntensityTier.Extreme;
            }

            if (score >= 55)
            {
                return CombatIntensityTier.High;
            }

            if (score >= 22)
            {
                return CombatIntensityTier.Medium;
            }

            return CombatIntensityTier.Low;
        }

        private static bool IsCriticalHealth(CombatActivitySample sample, float threshold)
        {
            return sample.HealthPercent > 0f && sample.HealthPercent <= threshold;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using ValheimSessionChronicle.Core;
using ValheimSessionChronicle.Models;
using ValheimSessionChronicle.Utility;

namespace ValheimSessionChronicle.Reporting
{
    public sealed class ChronicleStoryGenerator
    {
        // Local procedural story composition. No external service is used; the text is built from
        // grouped milestones, discoveries, combat aggregates, exploration, and survival stats.
        public string Generate(SessionData session, IReadOnlyList<SessionEvent> meaningfulEvents)
        {
            List<string> sentences = new List<string>();
            string mainPlayer = GetMainPlayer(session);

            AddBiomeSentence(session, mainPlayer, sentences);
            AddBuildingSentence(meaningfulEvents, sentences);
            AddDiscoverySentence(meaningfulEvents, sentences);
            AddCombatSentence(session, mainPlayer, sentences);
            AddBossSentence(session, sentences);
            AddEndingSentence(session, sentences);

            if (sentences.Count == 0)
            {
                sentences.Add($"{mainPlayer} odehrál klidnou session dlouhou {FormatDuration(session.Duration)} bez výrazných zlomů zaznamenaných klientem.");
            }

            return string.Join(" ", sentences);
        }

        private static void AddBiomeSentence(SessionData session, string mainPlayer, ICollection<string> sentences)
        {
            List<string> biomes = session.Environment.BiomesVisited
                .Where(ChronicleFilters.IsValidBiome)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(biome => session.Environment.FirstBiomeVisitUtc.TryGetValue(biome, out DateTime firstSeen) ? firstSeen : DateTime.MaxValue)
                .ToList();

            if (biomes.Count >= 2)
            {
                sentences.Add($"{mainPlayer} se během výpravy vydal z biomu {biomes.First()} až do {biomes.Last()}.");
            }
            else if (biomes.Count == 1)
            {
                sentences.Add($"{mainPlayer} strávil výpravu hlavně v biomu {biomes[0]}.");
            }
        }

        private static void AddBuildingSentence(IReadOnlyList<SessionEvent> meaningfulEvents, ICollection<string> sentences)
        {
            List<SessionEvent> building = meaningfulEvents
                .Where(entry => entry.Type == EventTypes.BuildingMilestone)
                .Take(3)
                .ToList();

            if (building.Count == 0)
            {
                return;
            }

            if (building.Count == 1)
            {
                sentences.Add(building[0].Description);
                return;
            }

            sentences.Add("Během cesty vzniklo několik důležitých zázemí: " +
                          string.Join(" ", building.Select(entry => entry.Description)));
        }

        private static void AddDiscoverySentence(IReadOnlyList<SessionEvent> meaningfulEvents, ICollection<string> sentences)
        {
            List<string> discoveries = meaningfulEvents
                .Where(entry => (entry.Type == EventTypes.Discovery || entry.Type == EventTypes.Crafting) &&
                                !string.IsNullOrWhiteSpace(entry.Target) &&
                                !ChronicleFilters.IsCommonResource(entry.Target))
                .Select(entry => entry.Target)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(5)
                .ToList();

            if (discoveries.Count == 0)
            {
                return;
            }

            sentences.Add(discoveries.Count == 1
                ? $"Nejdůležitějším nálezem session byl {discoveries[0]}."
                : $"Mezi důležité nálezy a milníky patřily {JoinCzech(discoveries)}.");
        }

        private static void AddCombatSentence(SessionData session, string mainPlayer, ICollection<string> sentences)
        {
            Dictionary<string, int> enemyKills = MergeCounts(session.PlayerStats.Values.Select(stats => stats.EnemyKills));
            int dangerousEncounters = session.PlayerStats.Values.Sum(stats => stats.DangerousEncounters);

            if (enemyKills.Count == 0 && dangerousEncounters == 0)
            {
                return;
            }

            if (enemyKills.Count > 0)
            {
                List<string> topKills = enemyKills
                    .OrderByDescending(pair => pair.Value)
                    .ThenBy(pair => pair.Key)
                    .Take(3)
                    .Select(pair => $"{pair.Value}x {pair.Key}")
                    .ToList();

                sentences.Add($"{mainPlayer} v boji porazil hlavně {JoinCzech(topKills)}.");
            }

            if (dangerousEncounters > 0 && session.Environment.NightTransitions > 0 && session.PlayerStats.Values.Sum(stats => stats.Deaths) == 0)
            {
                sentences.Add($"{mainPlayer} přežil nebezpečnou noční výpravu bez smrti.");
            }
            else if (dangerousEncounters > 0)
            {
                sentences.Add($"Výprava zahrnovala {dangerousEncounters} nebezpečných střetů, které stály za zapamatování.");
            }
        }

        private static void AddBossSentence(SessionData session, ICollection<string> sentences)
        {
            if (session.Environment.BossesKilled.Count == 0)
            {
                return;
            }

            sentences.Add(session.Environment.BossesKilled.Count == 1
                ? $"Vrcholným momentem bylo poražení bosse {session.Environment.BossesKilled[0]}."
                : $"Vrcholnými momenty bylo poražení bossů {JoinCzech(session.Environment.BossesKilled)}.");
        }

        private static void AddEndingSentence(SessionData session, ICollection<string> sentences)
        {
            int deaths = session.PlayerStats.Values.Sum(stats => stats.Deaths);
            string duration = FormatDuration(session.Duration);

            if (deaths == 0)
            {
                sentences.Add($"Výprava skončila bez smrti po {duration} dobrodružství.");
            }
            else if (deaths == 1)
            {
                sentences.Add($"Výprava trvala {duration} a vyžádala si jedno úmrtí.");
            }
            else
            {
                sentences.Add($"Výprava trvala {duration} a vyžádala si {deaths} úmrtí.");
            }
        }

        private static string GetMainPlayer(SessionData session)
        {
            if (!string.IsNullOrWhiteSpace(session.LocalPlayerName))
            {
                return session.LocalPlayerName;
            }

            return session.Players.FirstOrDefault() ?? "Hráč";
        }

        private static Dictionary<string, int> MergeCounts(IEnumerable<Dictionary<string, int>> dictionaries)
        {
            Dictionary<string, int> merged = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (Dictionary<string, int> dictionary in dictionaries)
            {
                foreach (KeyValuePair<string, int> pair in dictionary)
                {
                    merged.TryGetValue(pair.Key, out int current);
                    merged[pair.Key] = current + pair.Value;
                }
            }

            return merged;
        }

        private static string JoinCzech(IReadOnlyList<string> values)
        {
            if (values.Count == 0)
            {
                return string.Empty;
            }

            if (values.Count == 1)
            {
                return values[0];
            }

            return string.Join(", ", values.Take(values.Count - 1)) + " a " + values.Last();
        }

        private static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalHours >= 1)
            {
                return $"{(int)duration.TotalHours}h {duration.Minutes}m";
            }

            return $"{Math.Max(0, duration.Minutes)}m {Math.Max(0, duration.Seconds)}s";
        }
    }
}

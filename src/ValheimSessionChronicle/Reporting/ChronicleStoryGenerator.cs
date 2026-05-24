using System;
using System.Collections.Generic;
using System.Linq;
using ValheimSessionChronicle.Core;
using ValheimSessionChronicle.Models;
using ValheimSessionChronicle.Reporting.Analysis;
using ValheimSessionChronicle.Utility;
using ValheimSessionChronicle.WorldMemory;

namespace ValheimSessionChronicle.Reporting
{
    public sealed class ChronicleStoryGenerator
    {
        private readonly NarrativePhaseBuilder _phaseBuilder = new NarrativePhaseBuilder();

        // Local procedural story composition. No external service is used; the text is built from
        // grouped phases, intensity analysis, discoveries, camp classification, and survival stats.
        public string Generate(
            SessionData session,
            IReadOnlyList<SessionEvent> meaningfulEvents,
            CombatIntensityResult combat,
            SurvivalSummary survival,
            ExpeditionProfileResult profile,
            IReadOnlyList<CampCluster> camps,
            WorldMemoryData worldMemory,
            WorldMemoryUpdateResult memoryUpdate)
        {
            List<string> paragraphs = new List<string>();
            string mainPlayer = GetMainPlayer(session);

            List<string> phases = _phaseBuilder.BuildPhases(session, combat, survival, camps);
            if (phases.Count > 0)
            {
                paragraphs.Add(string.Join(" ", phases));
            }

            AddWorldMemoryParagraph(worldMemory, memoryUpdate, paragraphs);
            AddDiscoveryParagraph(meaningfulEvents, paragraphs);
            AddProfileParagraph(profile, paragraphs);
            AddCombatDetails(session, mainPlayer, combat, paragraphs);
            AddEndingParagraph(session, paragraphs);

            if (paragraphs.Count == 0)
            {
                paragraphs.Add($"{mainPlayer} odehrál klidnou session dlouhou {FormatDuration(session.Duration)} bez výrazných zlomů zachycených klientem.");
            }

            return string.Join(Environment.NewLine + Environment.NewLine, paragraphs);
        }

        private static void AddWorldMemoryParagraph(WorldMemoryData worldMemory, WorldMemoryUpdateResult memoryUpdate, ICollection<string> paragraphs)
        {
            if (memoryUpdate.CampChanges.Count == 0)
            {
                if (worldMemory.SessionCount > 1 && worldMemory.Camps.Count > 0)
                {
                    paragraphs.Add($"Kronika navazuje na dříve pozorovaný svět, ve kterém už je známo {worldMemory.Camps.Count} táborů nebo základen.");
                }

                return;
            }

            PersistentCampChange strongest = memoryUpdate.CampChanges
                .OrderByDescending(change => change.NewTier)
                .ThenByDescending(change => change.AddedStructures)
                .First();

            string biome = ChronicleFilters.IsValidBiome(strongest.Biome) ? $" v biomu {strongest.Biome}" : string.Empty;
            if (strongest.IsNewCamp)
            {
                paragraphs.Add($"Na mapě světa se objevil nový opěrný bod: {strongest.NewTierName}{biome}.");
                return;
            }

            if (strongest.IsTierUpgrade)
            {
                paragraphs.Add($"Dříve známý {strongest.PreviousTierName}{biome} se během session proměnil v {strongest.NewTierName}.");
                return;
            }

            List<string> additions = new List<string>();
            if (strongest.AddedForge)
            {
                additions.Add("kovárnu");
            }

            if (strongest.AddedDefenses)
            {
                additions.Add("obranné prvky");
            }

            if (strongest.AddedStorage)
            {
                additions.Add("skladování");
            }

            if (strongest.AddedPortal)
            {
                additions.Add("portál");
            }

            if (strongest.AddedAdvancedStation)
            {
                additions.Add("pokročilé řemeslné vybavení");
            }

            if (additions.Count > 0)
            {
                paragraphs.Add($"Skupina pokračovala v rozšiřování známého místa{biome} o {JoinCzech(additions)}.");
            }
            else if (strongest.AddedStructures > 0)
            {
                paragraphs.Add($"Známé zázemí{biome} bylo dál rozšířeno a upevněno.");
            }
        }

        private static void AddDiscoveryParagraph(IReadOnlyList<SessionEvent> meaningfulEvents, ICollection<string> paragraphs)
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

            paragraphs.Add(discoveries.Count == 1
                ? $"Nejdůležitějším nálezem session byl {discoveries[0]}."
                : $"Mezi důležité nálezy a milníky patřily {JoinCzech(discoveries)}.");
        }

        private static void AddProfileParagraph(ExpeditionProfileResult profile, ICollection<string> paragraphs)
        {
            List<ExpeditionProfileScore> dominant = profile.DominantScores.ToList();
            if (dominant.Count == 0)
            {
                return;
            }

            paragraphs.Add("Charakter výpravy nejvíc určovaly tyto prvky: " +
                           string.Join(", ", dominant.Select(score => $"{score.Label.ToLowerInvariant()} ({score.Percentage} %)")) + ".");
        }

        private static void AddCombatDetails(SessionData session, string mainPlayer, CombatIntensityResult combat, ICollection<string> paragraphs)
        {
            Dictionary<string, int> enemyKills = MergeCounts(session.PlayerStats.Values.Select(stats => stats.EnemyKills));
            if (enemyKills.Count == 0)
            {
                return;
            }

            List<string> topKills = enemyKills
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key)
                .Take(IsAtLeast(combat.Tier, CombatIntensityTier.High) ? 2 : 3)
                .Select(pair => $"{pair.Value}x {pair.Key}")
                .ToList();

            if (IsAtLeast(combat.Tier, CombatIntensityTier.High))
            {
                paragraphs.Add($"{mainPlayer} a okolní skupina čelili dlouhému tlaku nepřátel; nejčastěji padali {JoinCzech(topKills)}.");
            }
            else
            {
                paragraphs.Add($"{mainPlayer} během cesty porazil hlavně {JoinCzech(topKills)}.");
            }
        }

        private static void AddEndingParagraph(SessionData session, ICollection<string> paragraphs)
        {
            int deaths = session.PlayerStats.Values.Sum(stats => stats.Deaths);
            string duration = FormatDuration(session.Duration);

            if (deaths == 0)
            {
                paragraphs.Add($"Výprava skončila bez smrti po {duration} dobrodružství.");
            }
            else if (deaths == 1)
            {
                paragraphs.Add($"Výprava trvala {duration} a vyžádala si jedno úmrtí.");
            }
            else
            {
                paragraphs.Add($"Výprava trvala {duration} a vyžádala si {deaths} úmrtí.");
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

        private static bool IsAtLeast(CombatIntensityTier actual, CombatIntensityTier expected)
        {
            return (int)actual >= (int)expected;
        }
    }
}

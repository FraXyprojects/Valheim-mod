using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ValheimSessionChronicle.Core;
using ValheimSessionChronicle.Models;
using ValheimSessionChronicle.Reporting.Analysis;
using ValheimSessionChronicle.Utility;
using ValheimSessionChronicle.WorldMemory;

namespace ValheimSessionChronicle.Reporting
{
    public sealed class ReportGenerator
    {
        private const string Line = "==================================================";
        private const string SectionLine = "--------------------------------------------------";

        private readonly ChronicleStoryGenerator _storyGenerator = new ChronicleStoryGenerator();
        private readonly CombatIntensityAnalyzer _combatAnalyzer = new CombatIntensityAnalyzer();
        private readonly SurvivalAnalyzer _survivalAnalyzer = new SurvivalAnalyzer();
        private readonly ExpeditionProfileAnalyzer _profileAnalyzer = new ExpeditionProfileAnalyzer();
        private readonly CampClassificationSystem _campClassification = new CampClassificationSystem();
        private readonly ProgressionContextAnalyzer _progressionAnalyzer = new ProgressionContextAnalyzer();
        private readonly DiscoveryValueAnalyzer _discoveryAnalyzer = new DiscoveryValueAnalyzer();

        public string Generate(
            SessionData session,
            bool includeCompactTimeline,
            WorldMemoryData worldMemory,
            WorldMemoryUpdateResult memoryUpdate)
        {
            List<SessionEvent> meaningfulEvents = GetMeaningfulEvents(session, memoryUpdate).ToList();
            CombatIntensityResult combat = _combatAnalyzer.Analyze(session);
            SurvivalSummary survival = _survivalAnalyzer.Analyze(session);
            List<CampCluster> camps = _campClassification.Classify(session);
            ProgressionContext progression = _progressionAnalyzer.Analyze(session, worldMemory);
            DiscoveryAnalysis discovery = _discoveryAnalyzer.Analyze(session, worldMemory, memoryUpdate, progression);
            ExpeditionProfileResult profile = _profileAnalyzer.Analyze(session, combat, survival, discovery);
            StringBuilder builder = new StringBuilder(8192);
            DateTime localStart = session.StartTimeUtc.ToLocalTime();

            AppendHeader(builder);
            AppendMetadata(builder, session, localStart);
            AppendPlayers(builder, session);
            AppendStory(builder, session, meaningfulEvents, combat, survival, profile, camps, worldMemory, memoryUpdate, progression, discovery);
            AppendWorldContinuity(builder, worldMemory, memoryUpdate);
            AppendProgressionContext(builder, progression);
            AppendDiscoveryContext(builder, discovery);
            AppendExpeditionProfile(builder, profile);
            AppendHighlights(builder, meaningfulEvents);
            AppendStats(builder, session, combat, survival, camps);

            if (includeCompactTimeline)
            {
                AppendCompactTimeline(builder, meaningfulEvents);
            }

            AppendLimitations(builder);
            builder.AppendLine(Line);
            return builder.ToString();
        }

        private static IEnumerable<SessionEvent> GetMeaningfulEvents(SessionData session, WorldMemoryUpdateResult memoryUpdate)
        {
            return session.Events
                .Where(ChronicleFilters.ShouldAppearInChronicle)
                .Where(entry => !IsKnownWorldMemoryDuplicate(entry, memoryUpdate))
                .OrderBy(entry => entry.TimestampUtc);
        }

        private static bool IsKnownWorldMemoryDuplicate(SessionEvent entry, WorldMemoryUpdateResult memoryUpdate)
        {
            if (entry.Type == EventTypes.Discovery)
            {
                if (!string.IsNullOrWhiteSpace(entry.Biome) && memoryUpdate.PreviouslyKnownBiomes.Contains(entry.Biome))
                {
                    return true;
                }

                if (!string.IsNullOrWhiteSpace(entry.Target) && memoryUpdate.PreviouslyKnownImportantItems.Contains(entry.Target))
                {
                    return true;
                }
            }

            if (entry.Type == EventTypes.BossKilled && memoryUpdate.PreviouslyKnownBosses.Contains(entry.Target))
            {
                return true;
            }

            if (entry.Type == EventTypes.PortalUsed && memoryUpdate.NewPortals.Count == 0)
            {
                return true;
            }

            if (entry.Type == EventTypes.BuildingMilestone && !string.IsNullOrWhiteSpace(entry.Target))
            {
                bool knownStructure = memoryUpdate.PreviouslyKnownImportantStructures.Contains(entry.Target);
                bool newStructure = memoryUpdate.NewImportantStructures.Any(record =>
                    string.Equals(record.StructureName, entry.Target, StringComparison.OrdinalIgnoreCase));

                if (knownStructure && !newStructure)
                {
                    return true;
                }

                bool meaningfulCampChange = memoryUpdate.CampChanges.Any(change => change.IsNewCamp || change.IsTierUpgrade);
                if (ChronicleFilters.IsCampPiece(entry.Target) && !meaningfulCampChange)
                {
                    return true;
                }
            }

            return false;
        }

        private static void AppendHeader(StringBuilder builder)
        {
            builder.AppendLine(Line);
            builder.AppendLine("VALHEIM SESSION CHRONICLE");
            builder.AppendLine(Line);
            builder.AppendLine();
        }

        private static void AppendMetadata(StringBuilder builder, SessionData session, DateTime localStart)
        {
            builder.AppendLine(SectionLine);
            builder.AppendLine("SESSION");
            builder.AppendLine(SectionLine);
            builder.AppendLine();
            builder.AppendLine("Server: " + Fallback(session.ServerName, "Dedicated Server"));
            if (!string.IsNullOrWhiteSpace(session.WorldName))
            {
                builder.AppendLine("Svět: " + session.WorldName);
            }

            builder.AppendLine($"Datum: {localStart:dd.MM.yyyy}");
            builder.AppendLine($"Začátek: {localStart:HH:mm}");
            builder.AppendLine($"Konec: {session.EndTimeUtc.ToLocalTime():HH:mm}");
            builder.AppendLine($"Délka session: {FormatDuration(session.Duration)}");
            builder.AppendLine();
        }

        private static void AppendPlayers(StringBuilder builder, SessionData session)
        {
            builder.AppendLine(SectionLine);
            builder.AppendLine("HRÁČI");
            builder.AppendLine(SectionLine);
            builder.AppendLine();

            if (session.Players.Count == 0)
            {
                builder.AppendLine("- Nezjištěno");
            }
            else
            {
                foreach (string player in session.Players.OrderBy(player => player))
                {
                    builder.AppendLine("- " + player);
                }
            }

            builder.AppendLine();
        }

        private void AppendStory(
            StringBuilder builder,
            SessionData session,
            IReadOnlyList<SessionEvent> meaningfulEvents,
            CombatIntensityResult combat,
            SurvivalSummary survival,
            ExpeditionProfileResult profile,
            IReadOnlyList<CampCluster> camps,
            WorldMemoryData worldMemory,
            WorldMemoryUpdateResult memoryUpdate,
            ProgressionContext progression,
            DiscoveryAnalysis discovery)
        {
            builder.AppendLine(SectionLine);
            builder.AppendLine("PŘÍBĚH SESSION");
            builder.AppendLine(SectionLine);
            builder.AppendLine();
            builder.AppendLine(_storyGenerator.Generate(session, meaningfulEvents, combat, survival, profile, camps, worldMemory, memoryUpdate, progression, discovery));
            builder.AppendLine();
        }

        private static void AppendWorldContinuity(StringBuilder builder, WorldMemoryData worldMemory, WorldMemoryUpdateResult memoryUpdate)
        {
            builder.AppendLine(SectionLine);
            builder.AppendLine("PAMĚŤ SVĚTA");
            builder.AppendLine(SectionLine);
            builder.AppendLine();

            builder.AppendLine($"- Zaznamenané session v tomto světě: {worldMemory.SessionCount}");
            builder.AppendLine($"- Známé tábory/základny: {worldMemory.Camps.Count}");
            builder.AppendLine($"- Známé portály: {worldMemory.Portals.Count}");

            if (memoryUpdate.CampChanges.Count > 0)
            {
                builder.AppendLine("- Vývoj míst v této session:");
                foreach (PersistentCampChange change in memoryUpdate.CampChanges.Take(5))
                {
                    string biome = ChronicleFilters.IsValidBiome(change.Biome) ? $" v biomu {change.Biome}" : string.Empty;
                    if (change.IsNewCamp)
                    {
                        builder.AppendLine($"  - Nově zaznamenán {change.NewTierName}{biome}.");
                    }
                    else if (change.IsTierUpgrade)
                    {
                        builder.AppendLine($"  - {change.PreviousTierName} se rozrostl na {change.NewTierName}{biome}.");
                    }
                    else
                    {
                        builder.AppendLine($"  - {change.NewTierName}{biome} byl rozšířen o {change.AddedStructures} dílů.");
                    }
                }
            }

            builder.AppendLine();
        }

        private static void AppendProgressionContext(StringBuilder builder, ProgressionContext progression)
        {
            builder.AppendLine(SectionLine);
            builder.AppendLine("PROGRES SVĚTA");
            builder.AppendLine(SectionLine);
            builder.AppendLine();

            builder.AppendLine(progression.HasStrongEvidence
                ? $"- Dominantní fáze podle pozorování: {progression.DominantLabel}"
                : "- Dominantní fáze není jistá; klient zachytil jen slabé indicie.");

            foreach (ProgressionConfidence confidence in progression.Confidences.Take(7))
            {
                builder.AppendLine($"- {confidence.Label}: {confidence.Percentage}%");
            }

            if (progression.Evidence.Count > 0)
            {
                builder.AppendLine("- Hlavní indicie:");
                foreach (string evidence in progression.Evidence.Take(5))
                {
                    builder.AppendLine($"  - {evidence}");
                }
            }

            builder.AppendLine();
        }

        private static void AppendDiscoveryContext(StringBuilder builder, DiscoveryAnalysis discovery)
        {
            if (!discovery.HasMeaningfulContent)
            {
                return;
            }

            builder.AppendLine(SectionLine);
            builder.AppendLine("OPERACE A OBJEVY");
            builder.AppendLine(SectionLine);
            builder.AppendLine();

            if (discovery.ResourceOperations.Count > 0)
            {
                builder.AppendLine("Resource operace:");
                foreach (ResourceOperation operation in discovery.ResourceOperations.Take(5))
                {
                    builder.AppendLine($"- {operation.OperationType}: {operation.Summary}");
                }

                builder.AppendLine();
            }

            if (discovery.Discoveries.Count > 0)
            {
                builder.AppendLine("Významné objevy:");
                foreach (DiscoveryValueRecord record in discovery.Discoveries.Take(5))
                {
                    builder.AppendLine($"- {record.Summary}");
                }

                builder.AppendLine();
            }
        }

        private static void AppendExpeditionProfile(StringBuilder builder, ExpeditionProfileResult profile)
        {
            builder.AppendLine(SectionLine);
            builder.AppendLine("CHARAKTER VÝPRAVY");
            builder.AppendLine(SectionLine);
            builder.AppendLine();

            if (profile.Scores.Count == 0)
            {
                builder.AppendLine("- Nedostatek dat pro profil výpravy.");
            }
            else
            {
                foreach (ExpeditionProfileScore score in profile.Scores.Where(score => score.Percentage >= 3).Take(8))
                {
                    builder.AppendLine($"- {score.Label}: {score.Percentage}%");
                }
            }

            builder.AppendLine();
        }

        private static void AppendHighlights(StringBuilder builder, IReadOnlyList<SessionEvent> meaningfulEvents)
        {
            builder.AppendLine(SectionLine);
            builder.AppendLine("HLAVNÍ MOMENTY");
            builder.AppendLine(SectionLine);
            builder.AppendLine();

            List<SessionEvent> highlights = meaningfulEvents
                .Where(entry => entry.Category != EventCategories.Session &&
                                entry.Type != EventTypes.PlayerJoined &&
                                entry.Type != EventTypes.PlayerRespawn)
                .OrderByDescending(entry => entry.Importance)
                .ThenBy(entry => entry.TimestampUtc)
                .Take(12)
                .OrderBy(entry => entry.TimestampUtc)
                .ToList();

            if (highlights.Count == 0)
            {
                builder.AppendLine("- Žádný výrazný moment nebyl klientem zachycen.");
            }
            else
            {
                foreach (SessionEvent entry in highlights)
                {
                    builder.AppendLine($"- [{entry.TimestampUtc.ToLocalTime():HH:mm}] {entry.Description}");
                }
            }

            builder.AppendLine();
        }

        private static void AppendStats(
            StringBuilder builder,
            SessionData session,
            CombatIntensityResult combat,
            SurvivalSummary survival,
            IReadOnlyList<CampCluster> camps)
        {
            builder.AppendLine(SectionLine);
            builder.AppendLine("STATISTIKY");
            builder.AppendLine(SectionLine);
            builder.AppendLine();

            AppendDeaths(builder, session);
            AppendExploration(builder, session);
            AppendCombat(builder, session, combat, survival);
            AppendBuilding(builder, session, camps);
            AppendCrafting(builder, session);
            AppendTravel(builder, session);
            AppendEnvironment(builder, session);
        }

        private static void AppendDeaths(StringBuilder builder, SessionData session)
        {
            builder.AppendLine("Úmrtí:");
            List<PlayerStats> statsWithDeaths = session.PlayerStats.Values
                .Where(stats => stats.Deaths > 0)
                .OrderByDescending(stats => stats.Deaths)
                .ToList();

            if (statsWithDeaths.Count == 0)
            {
                builder.AppendLine("- Bez zaznamenané smrti");
            }
            else
            {
                foreach (PlayerStats stats in statsWithDeaths)
                {
                    builder.AppendLine($"- {stats.PlayerName}: {stats.Deaths}");
                }
            }

            builder.AppendLine();
        }

        private static void AppendExploration(StringBuilder builder, SessionData session)
        {
            builder.AppendLine("Průzkum:");
            List<string> biomes = session.Environment.BiomesVisited
                .Where(ChronicleFilters.IsValidBiome)
                .OrderBy(name => name)
                .ToList();

            if (biomes.Count == 0)
            {
                builder.AppendLine("- Biomy nebyly spolehlivě zjištěny");
            }
            else
            {
                builder.AppendLine("- Navštívené biomy: " + string.Join(", ", biomes));
            }

            if (session.Environment.OutpostBiomes.Count > 0)
            {
                builder.AppendLine("- Založené opěrné body: " + string.Join(", ", session.Environment.OutpostBiomes.OrderBy(name => name)));
            }

            builder.AppendLine();
        }

        private static void AppendCombat(StringBuilder builder, SessionData session, CombatIntensityResult combat, SurvivalSummary survival)
        {
            Dictionary<string, int> enemyKills = TopCounts(session.PlayerStats.Values.Select(stats => stats.EnemyKills), 8);
            int localKills = session.PlayerStats.Values.Sum(stats => stats.EnemiesKilled);
            int dangerousEncounters = session.PlayerStats.Values.Sum(stats => stats.DangerousEncounters);

            builder.AppendLine("Boj:");
            builder.AppendLine($"- Potvrzená lokální zabití nepřátel: {localKills}");
            builder.AppendLine($"- Nebezpečné střety: {dangerousEncounters}");
            builder.AppendLine($"- Intenzita boje: {TranslateCombatTier(combat.Tier)} ({combat.Score:0})");
            if (ChronicleFilters.IsValidBiome(combat.DominantCombatBiome))
            {
                builder.AppendLine($"- Nejtvrdší bojový biome: {combat.DominantCombatBiome}");
            }

            if (survival.HasHealthData)
            {
                builder.AppendLine($"- Nejnižší zaznamenané HP: {survival.LowestHealth:0} ({survival.LowestHealthPercent:P0})");
                builder.AppendLine($"- Near-death momenty: {survival.NearDeathMoments}");
                builder.AppendLine($"- Největší jednotlivý zásah: {survival.LargestSingleHit:0} ({survival.LargestSingleHitPercent:P0})");
                builder.AppendLine($"- Survival stress: {TranslateCombatTier(survival.StressTier)} ({survival.CombatStressScore:0})");
                if (survival.HeroicEscapes > 0)
                {
                    builder.AppendLine($"- Heroic escape: {survival.HeroicEscapes}x");
                }

                if (survival.LastStandMoments > 0)
                {
                    builder.AppendLine($"- Last stand momenty: {survival.LastStandMoments}x");
                }

                if (survival.FiercestWindow != null)
                {
                    builder.AppendLine($"- Nejtvrdší bojové okno: {survival.FiercestWindow.Kills} zabití, {survival.FiercestWindow.IncomingHits} zásahů, minimum HP {survival.FiercestWindow.LowestHealthPercent:P0}");
                }
            }
            else
            {
                builder.AppendLine("- HP tracking: nedostatek spolehlivých klientských dat");
            }

            if (session.Environment.BossesKilled.Count > 0)
            {
                builder.AppendLine("- Poražení bossové: " + string.Join(", ", session.Environment.BossesKilled.OrderBy(name => name)));
            }

            AppendTopDictionary(builder, enemyKills, "Nejčastěji poražení nepřátelé");
            AppendBiomeCombat(builder, session);
            builder.AppendLine();
        }

        private static void AppendBiomeCombat(StringBuilder builder, SessionData session)
        {
            Dictionary<string, int> totalsByBiome = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (PlayerStats stats in session.PlayerStats.Values)
            {
                foreach (KeyValuePair<string, Dictionary<string, int>> biome in stats.EnemyKillsByBiome)
                {
                    if (!ChronicleFilters.IsValidBiome(biome.Key))
                    {
                        continue;
                    }

                    totalsByBiome.TryGetValue(biome.Key, out int current);
                    totalsByBiome[biome.Key] = current + biome.Value.Values.Sum();
                }
            }

            if (totalsByBiome.Count == 0)
            {
                return;
            }

            builder.AppendLine("- Aktivita podle biomů:");
            foreach (KeyValuePair<string, int> pair in totalsByBiome.OrderByDescending(pair => pair.Value).Take(5))
            {
                builder.AppendLine($"  - {pair.Key}: {pair.Value} zabití");
            }
        }

        private static void AppendBuilding(StringBuilder builder, SessionData session, IReadOnlyList<CampCluster> camps)
        {
            int pieces = session.PlayerStats.Values.Sum(stats => stats.PiecesPlacedTotal);
            int workstations = session.PlayerStats.Values.Sum(stats => stats.WorkstationsPlaced);
            builder.AppendLine("Stavba:");
            builder.AppendLine($"- Postavené díly: {pieces}");
            builder.AppendLine($"- Řemeslné stanice: {workstations}");
            if (camps.Count > 0)
            {
                builder.AppendLine("- Klasifikované tábory/základny:");
                foreach (CampCluster camp in camps.Take(5))
                {
                    string biome = ChronicleFilters.IsValidBiome(camp.Biome) ? $" ({camp.Biome})" : string.Empty;
                    builder.AppendLine($"  - {camp.Name}{biome}: {camp.StructureCount} dílů");
                }
            }
            builder.AppendLine();
        }

        private static void AppendCrafting(StringBuilder builder, SessionData session)
        {
            int crafts = session.PlayerStats.Values.Sum(stats => stats.Crafts);
            Dictionary<string, int> importantCrafts = TopCounts(
                session.PlayerStats.Values.Select(stats => stats.CraftedItems
                    .Where(pair => ValheimNames.IsImportantCraftingMilestone(pair.Key))
                    .ToDictionary(pair => pair.Key, pair => pair.Value)),
                5);

            builder.AppendLine("Crafting:");
            builder.AppendLine($"- Vyrobeno předmětů: {crafts}");
            AppendTopDictionary(builder, importantCrafts, "Důležité craftovací milníky");
            builder.AppendLine();
        }

        private static void AppendTravel(StringBuilder builder, SessionData session)
        {
            int portalUses = session.PlayerStats.Values.Sum(stats => stats.PortalUses);
            int shipUses = session.PlayerStats.Values.Sum(stats => stats.ShipUses);

            builder.AppendLine("Cestování:");
            builder.AppendLine($"- Portály použity: {portalUses}x");
            builder.AppendLine($"- Použití lodního kormidla: {shipUses}x");
            builder.AppendLine();
        }

        private static void AppendEnvironment(StringBuilder builder, SessionData session)
        {
            builder.AppendLine("Prostředí:");
            builder.AppendLine($"- Změny počasí: {session.Environment.WeatherChanges}");
            builder.AppendLine($"- Přechody do dne: {session.Environment.DayTransitions}");
            builder.AppendLine($"- Přechody do noci: {session.Environment.NightTransitions}");

            if (session.Environment.WeatherSeen.Count > 0)
            {
                builder.AppendLine("- Zaznamenané počasí: " + string.Join(", ", session.Environment.WeatherSeen.OrderBy(name => name)));
            }

            builder.AppendLine();
        }

        private static void AppendCompactTimeline(StringBuilder builder, IReadOnlyList<SessionEvent> meaningfulEvents)
        {
            builder.AppendLine(SectionLine);
            builder.AppendLine("KOMPAKTNÍ TIMELINE");
            builder.AppendLine(SectionLine);
            builder.AppendLine();

            List<SessionEvent> timeline = meaningfulEvents
                .Where(entry => entry.Category != EventCategories.Session &&
                                entry.Type != EventTypes.PlayerJoined &&
                                entry.Type != EventTypes.PlayerRespawn)
                .Take(20)
                .ToList();

            if (timeline.Count == 0)
            {
                builder.AppendLine("- Bez výrazných časových bodů.");
            }
            else
            {
                foreach (SessionEvent entry in timeline)
                {
                    builder.AppendLine($"[{entry.TimestampUtc.ToLocalTime():HH:mm}] {entry.Description}");
                }
            }

            builder.AppendLine();
        }

        private static void AppendLimitations(StringBuilder builder)
        {
            builder.AppendLine(SectionLine);
            builder.AppendLine("POZNÁMKA");
            builder.AppendLine(SectionLine);
            builder.AppendLine();
            builder.AppendLine("Tento chronicle vznikl pouze z klientských dat. Události mimo dohled klienta nebo události, které server klientovi neposlal, nemusí být kompletní.");
            builder.AppendLine();
        }

        private static Dictionary<string, int> TopCounts(IEnumerable<Dictionary<string, int>> dictionaries, int limit)
        {
            Dictionary<string, int> totals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (Dictionary<string, int> dictionary in dictionaries)
            {
                foreach (KeyValuePair<string, int> pair in dictionary)
                {
                    totals.TryGetValue(pair.Key, out int current);
                    totals[pair.Key] = current + pair.Value;
                }
            }

            return totals
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key)
                .Take(limit)
                .ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        private static void AppendTopDictionary(StringBuilder builder, Dictionary<string, int> values, string title)
        {
            if (values.Count == 0)
            {
                return;
            }

            builder.AppendLine($"- {title}:");
            foreach (KeyValuePair<string, int> pair in values)
            {
                builder.AppendLine($"  - {pair.Key}: {pair.Value}x");
            }
        }

        private static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalHours >= 1)
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}h {1}m", (int)duration.TotalHours, duration.Minutes);
            }

            return string.Format(CultureInfo.InvariantCulture, "{0}m {1}s", Math.Max(0, duration.Minutes), Math.Max(0, duration.Seconds));
        }

        private static string Fallback(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        private static string TranslateCombatTier(CombatIntensityTier tier)
        {
            switch (tier)
            {
                case CombatIntensityTier.Extreme:
                    return "extrémní";
                case CombatIntensityTier.High:
                    return "vysoká";
                case CombatIntensityTier.Medium:
                    return "střední";
                default:
                    return "nízká";
            }
        }
    }
}

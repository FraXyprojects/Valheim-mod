using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ValheimSessionChronicle.Core;
using ValheimSessionChronicle.Models;
using ValheimSessionChronicle.Utility;

namespace ValheimSessionChronicle.Reporting
{
    public sealed class ReportGenerator
    {
        private const string Line = "==================================================";
        private const string SectionLine = "--------------------------------------------------";

        private readonly ChronicleStoryGenerator _storyGenerator = new ChronicleStoryGenerator();

        public string Generate(SessionData session, bool includeCompactTimeline)
        {
            List<SessionEvent> meaningfulEvents = GetMeaningfulEvents(session).ToList();
            StringBuilder builder = new StringBuilder(8192);
            DateTime localStart = session.StartTimeUtc.ToLocalTime();

            AppendHeader(builder);
            AppendMetadata(builder, session, localStart);
            AppendPlayers(builder, session);
            AppendStory(builder, session, meaningfulEvents);
            AppendHighlights(builder, meaningfulEvents);
            AppendStats(builder, session);

            if (includeCompactTimeline)
            {
                AppendCompactTimeline(builder, meaningfulEvents);
            }

            AppendLimitations(builder);
            builder.AppendLine(Line);
            return builder.ToString();
        }

        private static IEnumerable<SessionEvent> GetMeaningfulEvents(SessionData session)
        {
            return session.Events
                .Where(ChronicleFilters.ShouldAppearInChronicle)
                .OrderBy(entry => entry.TimestampUtc);
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

        private void AppendStory(StringBuilder builder, SessionData session, IReadOnlyList<SessionEvent> meaningfulEvents)
        {
            builder.AppendLine(SectionLine);
            builder.AppendLine("PŘÍBĚH SESSION");
            builder.AppendLine(SectionLine);
            builder.AppendLine();
            builder.AppendLine(_storyGenerator.Generate(session, meaningfulEvents));
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

        private static void AppendStats(StringBuilder builder, SessionData session)
        {
            builder.AppendLine(SectionLine);
            builder.AppendLine("STATISTIKY");
            builder.AppendLine(SectionLine);
            builder.AppendLine();

            AppendDeaths(builder, session);
            AppendExploration(builder, session);
            AppendCombat(builder, session);
            AppendBuilding(builder, session);
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

        private static void AppendCombat(StringBuilder builder, SessionData session)
        {
            Dictionary<string, int> enemyKills = TopCounts(session.PlayerStats.Values.Select(stats => stats.EnemyKills), 8);
            int localKills = session.PlayerStats.Values.Sum(stats => stats.EnemiesKilled);
            int dangerousEncounters = session.PlayerStats.Values.Sum(stats => stats.DangerousEncounters);

            builder.AppendLine("Boj:");
            builder.AppendLine($"- Potvrzená lokální zabití nepřátel: {localKills}");
            builder.AppendLine($"- Nebezpečné střety: {dangerousEncounters}");

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

        private static void AppendBuilding(StringBuilder builder, SessionData session)
        {
            int pieces = session.PlayerStats.Values.Sum(stats => stats.PiecesPlacedTotal);
            int workstations = session.PlayerStats.Values.Sum(stats => stats.WorkstationsPlaced);
            builder.AppendLine("Stavba:");
            builder.AppendLine($"- Postavené díly: {pieces}");
            builder.AppendLine($"- Řemeslné stanice: {workstations}");
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
    }
}

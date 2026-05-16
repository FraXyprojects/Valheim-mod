using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ValheimSessionChronicle.Models;

namespace ValheimSessionChronicle.Reporting
{
    public sealed class ReportGenerator
    {
        private const string Line = "==================================================";
        private const string SectionLine = "--------------------------------------------------";

        public string Generate(SessionData session)
        {
            StringBuilder builder = new StringBuilder(8192);
            DateTime localStart = session.StartTimeUtc.ToLocalTime();

            builder.AppendLine(Line);
            builder.AppendLine("VALHEIM SESSION REPORT");
            builder.AppendLine(Line);
            builder.AppendLine();
            builder.AppendLine($"Server: {Fallback(session.ServerName, "Dedicated Server")}");
            if (!string.IsNullOrWhiteSpace(session.WorldName))
            {
                builder.AppendLine($"Svět: {session.WorldName}");
            }

            builder.AppendLine($"Datum: {localStart:dd.MM.yyyy}");
            builder.AppendLine($"Začátek: {localStart:HH:mm}");
            builder.AppendLine($"Konec: {session.EndTimeUtc.ToLocalTime():HH:mm}");
            builder.AppendLine($"Délka session: {FormatDuration(session.Duration)}");
            builder.AppendLine();

            AppendPlayers(builder, session);
            AppendMainEvents(builder, session);
            AppendStats(builder, session);
            AppendLimitations(builder);

            builder.AppendLine(Line);
            return builder.ToString();
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

        private static void AppendMainEvents(StringBuilder builder, SessionData session)
        {
            builder.AppendLine(SectionLine);
            builder.AppendLine("HLAVNÍ UDÁLOSTI");
            builder.AppendLine(SectionLine);
            builder.AppendLine();

            List<SessionEvent> events = session.Events
                .OrderBy(entry => entry.TimestampUtc)
                .ToList();

            if (events.Count == 0)
            {
                builder.AppendLine("Během session nebyly zaznamenány žádné události.");
                builder.AppendLine();
                return;
            }

            foreach (SessionEvent entry in events)
            {
                builder.AppendLine($"[{entry.TimestampUtc.ToLocalTime():HH:mm}]");
                builder.AppendLine(entry.Description);
                builder.AppendLine();
            }
        }

        private static void AppendStats(StringBuilder builder, SessionData session)
        {
            builder.AppendLine(SectionLine);
            builder.AppendLine("STATISTIKY");
            builder.AppendLine(SectionLine);
            builder.AppendLine();

            AppendDeaths(builder, session);
            AppendPortals(builder, session);
            AppendBiomes(builder, session);
            AppendBosses(builder, session);
            AppendBuilding(builder, session);
            AppendCrafting(builder, session);
            AppendCombat(builder, session);
            AppendEnvironment(builder, session);
        }

        private static void AppendDeaths(StringBuilder builder, SessionData session)
        {
            builder.AppendLine("Úmrtí:");
            IEnumerable<PlayerStats> statsWithDeaths = session.PlayerStats.Values
                .Where(stats => stats.Deaths > 0)
                .OrderByDescending(stats => stats.Deaths);

            if (!statsWithDeaths.Any())
            {
                builder.AppendLine("- Žádná zaznamenaná úmrtí");
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

        private static void AppendPortals(StringBuilder builder, SessionData session)
        {
            int portalUses = session.PlayerStats.Values.Sum(stats => stats.PortalUses);
            builder.AppendLine("Portály:");
            builder.AppendLine($"- Použito {portalUses}x");
            builder.AppendLine();
        }

        private static void AppendBiomes(StringBuilder builder, SessionData session)
        {
            builder.AppendLine("Biomy:");
            if (session.Environment.BiomesVisited.Count == 0)
            {
                builder.AppendLine("- Nezjištěno");
            }
            else
            {
                foreach (string biome in session.Environment.BiomesVisited.OrderBy(name => name))
                {
                    builder.AppendLine("- " + biome);
                }
            }

            builder.AppendLine();
        }

        private static void AppendBosses(StringBuilder builder, SessionData session)
        {
            builder.AppendLine("Bossové:");
            if (session.Environment.BossesKilled.Count == 0)
            {
                builder.AppendLine("- Žádný zaznamenaný boss kill");
            }
            else
            {
                foreach (string boss in session.Environment.BossesKilled.OrderBy(name => name))
                {
                    builder.AppendLine("- " + boss);
                }
            }

            builder.AppendLine();
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
            builder.AppendLine("Crafting:");
            builder.AppendLine($"- Vyrobeno předmětů: {crafts}");
            AppendTopDictionary(builder, TopCounts(session.PlayerStats.Values.Select(stats => stats.CraftedItems)), "Nejčastější výroba");
            builder.AppendLine();
        }

        private static void AppendCombat(StringBuilder builder, SessionData session)
        {
            int localKills = session.PlayerStats.Values.Sum(stats => stats.EnemiesKilled);
            int bossKills = session.PlayerStats.Values.Sum(stats => stats.BossesKilled);
            builder.AppendLine("Boj:");
            builder.AppendLine($"- Pravděpodobná lokální zabití nepřátel: {localKills}");
            builder.AppendLine($"- Boss kill eventy: {bossKills}");
            builder.AppendLine($"- Viditelná úmrtí nepřátel bez potvrzeného útočníka: {session.Environment.VisibleEnemyDeaths}");
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
                builder.AppendLine("- Zaznamenané počasí:");
                foreach (string weather in session.Environment.WeatherSeen.OrderBy(name => name))
                {
                    builder.AppendLine("  - " + weather);
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
            builder.AppendLine("Tento report vznikl pouze z klientských dat. Události mimo dohled klienta nebo události, které server klientovi neposlal, nemusí být kompletní.");
            builder.AppendLine();
        }

        private static Dictionary<string, int> TopCounts(IEnumerable<Dictionary<string, int>> dictionaries)
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
                .Take(5)
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

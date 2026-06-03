using System;
using System.Collections.Generic;
using System.Linq;
using ValheimSessionChronicle.Core;
using ValheimSessionChronicle.Models;
using ValheimSessionChronicle.Utility;

namespace ValheimSessionChronicle.Reporting.Analysis
{
    public sealed class ExpeditionProfileAnalyzer
    {
        public ExpeditionProfileResult Analyze(
            SessionData session,
            CombatIntensityResult combat,
            SurvivalSummary survival,
            DiscoveryAnalysis discovery = null)
        {
            int kills = session.PlayerStats.Values.Sum(stats => stats.EnemiesKilled);
            int pieces = session.PlayerStats.Values.Sum(stats => stats.PiecesPlacedTotal);
            int workstations = session.PlayerStats.Values.Sum(stats => stats.WorkstationsPlaced);
            int crafts = session.PlayerStats.Values.Sum(stats => stats.Crafts);
            int shipUses = session.PlayerStats.Values.Sum(stats => stats.ShipUses);
            int portalUses = session.PlayerStats.Values.Sum(stats => stats.PortalUses);
            int items = session.PlayerStats.Values.Sum(stats => stats.ItemsPickedUp);
            int bossKills = session.PlayerStats.Values.Sum(stats => stats.BossesKilled);
            int dangerous = session.PlayerStats.Values.Sum(stats => stats.DangerousEncounters);
            int discoveries = session.Events.Count(entry => entry.Type == EventTypes.Discovery);
            int biomes = session.Environment.BiomesVisited.Count(ChronicleFilters.IsValidBiome);
            int distinctPortals = session.PortalSamples
                .Where(sample => !string.IsNullOrWhiteSpace(sample.PortalName))
                .Select(sample => sample.PortalName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();
            int resourceOperations = discovery?.ResourceOperations.Sum(operation => Math.Min(80, operation.Quantity)) ?? 0;

            List<ExpeditionProfileScore> scores = new List<ExpeditionProfileScore>
            {
                Score("Bojová", "Bojova", kills * 2.0 + dangerous * 5.0 + combat.Score * 0.8 + bossKills * 10.0),
                Score("Průzkumná", "Pruzkumna", biomes * 14.0 + discoveries * 5.0 + session.Environment.PortalUses * 1.5),
                Score("Stavitelská", "Stavitelska", pieces * 0.45 + workstations * 5.0 + session.Environment.OutpostBiomes.Count * 8.0),
                Score("Námořní", "Namorni", shipUses * 14.0 + session.Environment.BiomesVisited.Count(biome => string.Equals(biome, "Ocean", StringComparison.OrdinalIgnoreCase)) * 15.0),
                Score("Craftovací", "Craftovaci", crafts * 1.0 + CountImportantCrafts(session) * 6.0),
                Score("Survival", "Survival", survival.NearDeathMoments * 12.0 + survival.IncomingHits * 1.4 + survival.Deaths * 8.0 + dangerous * 4.0),
                Score("Bossing", "Bossing", bossKills * 35.0),
                Score("Sběračská", "Sberacska", items * 0.35 + CountImportantPickups(session) * 6.0),
                Score("Rybářská", "Rybarska", CountKeywordItems(session, "fish", "ryba") * 10.0),
                Score("Farmářská", "Farmarska", CountKeywordItems(session, "carrot", "turnip", "onion", "barley", "flax", "mrkev", "repa", "cibule") * 6.0)
            };

            scores.Add(Score("Logistická", "Logisticka", portalUses * 6.0 + distinctPortals * 10.0 + session.PortalSamples.Count * 2.0));

            ExpeditionProfileScore gathering = scores.FirstOrDefault(score => score.FileToken == "Sberacska");
            if (gathering != null)
            {
                gathering.RawScore += resourceOperations * 0.35;
            }

            Normalize(scores);
            return new ExpeditionProfileResult { Scores = scores.Where(score => score.Percentage > 0).OrderByDescending(score => score.Percentage).ToList() };
        }

        private static ExpeditionProfileScore Score(string label, string fileToken, double rawScore)
        {
            return new ExpeditionProfileScore { Label = label, FileToken = fileToken, RawScore = Math.Max(0, rawScore) };
        }

        private static void Normalize(IReadOnlyList<ExpeditionProfileScore> scores)
        {
            double total = scores.Sum(score => score.RawScore);
            if (total <= 0)
            {
                return;
            }

            foreach (ExpeditionProfileScore score in scores)
            {
                score.Percentage = (int)Math.Round(score.RawScore / total * 100.0);
            }
        }

        private static int CountImportantCrafts(SessionData session)
        {
            return session.PlayerStats.Values.Sum(stats => stats.CraftedItems.Keys.Count(ValheimNames.IsImportantCraftingMilestone));
        }

        private static int CountImportantPickups(SessionData session)
        {
            return session.PlayerStats.Values.Sum(stats => stats.ItemPickups.Keys.Count(ValheimNames.IsImportantItem));
        }

        private static int CountKeywordItems(SessionData session, params string[] keywords)
        {
            return session.PlayerStats.Values.Sum(stats => stats.ItemPickups
                .Where(pair => keywords.Any(keyword => pair.Key.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0))
                .Sum(pair => pair.Value));
        }
    }
}

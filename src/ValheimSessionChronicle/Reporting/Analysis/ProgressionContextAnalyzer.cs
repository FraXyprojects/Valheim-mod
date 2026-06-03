using System;
using System.Collections.Generic;
using System.Linq;
using ValheimSessionChronicle.Models;
using ValheimSessionChronicle.Utility;
using ValheimSessionChronicle.WorldMemory;

namespace ValheimSessionChronicle.Reporting.Analysis
{
    public sealed class ProgressionContextAnalyzer
    {
        private sealed class StageRule
        {
            public ProgressionStage Stage { get; set; }
            public string Label { get; set; } = string.Empty;
            public string[] ItemTokens { get; set; } = new string[0];
            public string[] StructureTokens { get; set; } = new string[0];
            public string[] BossTokens { get; set; } = new string[0];
            public string[] BiomeTokens { get; set; } = new string[0];
        }

        private static readonly StageRule[] Rules =
        {
            new StageRule
            {
                Stage = ProgressionStage.BlackForest,
                Label = "Black Forest",
                ItemTokens = new[] { "bronze", "bronz", "finewoodbow", "lukzjemnehodreva", "surtlingcore", "ancientseed", "troll" },
                StructureTokens = new[] { "forge", "kovarna" },
                BossTokens = new[] { "elder" },
                BiomeTokens = new[] { "blackforest" }
            },
            new StageRule
            {
                Stage = ProgressionStage.Swamp,
                Label = "Swamp",
                ItemTokens = new[] { "iron", "zelezo", "ironnails", "cryptkey", "swampkey", "wishbone", "witheredbone", "root" },
                StructureTokens = new[] { "stonecutter", "kamenik" },
                BossTokens = new[] { "bonemass" },
                BiomeTokens = new[] { "swamp" }
            },
            new StageRule
            {
                Stage = ProgressionStage.Mountain,
                Label = "Mountain",
                ItemTokens = new[] { "silver", "stribro", "wolf", "vlci", "wolffang", "frostner", "dragonegg", "obsidian", "freezegland" },
                StructureTokens = new[] { "artisan" },
                BossTokens = new[] { "moder", "dragon" },
                BiomeTokens = new[] { "mountain" }
            },
            new StageRule
            {
                Stage = ProgressionStage.Plains,
                Label = "Plains",
                ItemTokens = new[] { "blackmetal", "cernykov", "padded", "lox", "barley", "flax", "totem" },
                StructureTokens = new[] { "blastfurnace", "windmill", "spinningwheel" },
                BossTokens = new[] { "yagluth" },
                BiomeTokens = new[] { "plains" }
            },
            new StageRule
            {
                Stage = ProgressionStage.Mistlands,
                Label = "Mistlands",
                ItemTokens = new[] { "yggdrasil", "sap", "blackcore", "refinedeitr", "eitr", "carapace", "softtissue", "dvergrextractor", "mandible", "sealbreaker" },
                StructureTokens = new[] { "blackforge", "galdr", "eitrrefinery" },
                BossTokens = new[] { "queen" },
                BiomeTokens = new[] { "mistlands" }
            },
            new StageRule
            {
                Stage = ProgressionStage.Ashlands,
                Label = "Ashlands",
                ItemTokens = new[] { "flametal", "ashwood", "asksvin", "morgen", "charred", "grausten" },
                StructureTokens = new[] { "shieldgenerator", "siege" },
                BossTokens = new[] { "fader" },
                BiomeTokens = new[] { "ashlands" }
            }
        };

        public ProgressionContext Analyze(SessionData session, WorldMemoryData worldMemory)
        {
            Dictionary<ProgressionStage, double> scores = CreateScoreMap();
            List<string> evidence = new List<string>();

            AddItemEvidence(scores, evidence, CollectPickups(session), 1.0, "zisk");
            AddItemEvidence(scores, evidence, CollectCrafts(session), 1.2, "crafting");
            AddItemEvidence(scores, evidence, session.ObservedInventoryItems, 1.4, "inventář");
            AddItemEvidence(scores, evidence, session.ObservedContainerItems, 2.4, "zásoby v truhlách");
            AddMemoryItemEvidence(scores, evidence, worldMemory);
            AddStructureEvidence(scores, evidence, session.BuildSamples.Select(sample => sample.PieceName), 13.0, "postavená stanice");
            AddStructureEvidence(scores, evidence, session.StructureObservations.Select(sample => sample.StructureName), 17.0, "blízká stanice");
            AddMemoryStructureEvidence(scores, evidence, worldMemory);
            AddBossEvidence(scores, evidence, session.Environment.BossesKilled, 26.0, "poražený boss");
            AddBossEvidence(scores, evidence, worldMemory?.BossesDefeated, 22.0, "dříve známý boss");
            AddBiomeEvidence(scores, evidence, session.Environment.BiomesVisited, 4.0, "navštívený biome");
            AddBiomeEvidence(scores, evidence, worldMemory?.DiscoveredBiomes, 2.5, "známý biome");

            return BuildContext(scores, evidence);
        }

        private static Dictionary<ProgressionStage, double> CreateScoreMap()
        {
            Dictionary<ProgressionStage, double> scores = new Dictionary<ProgressionStage, double>();
            foreach (ProgressionStage stage in Enum.GetValues(typeof(ProgressionStage)))
            {
                scores[stage] = 0;
            }

            return scores;
        }

        private static Dictionary<string, int> CollectPickups(SessionData session)
        {
            return MergeCounts(session.PlayerStats.Values.Select(stats => stats.ItemPickups));
        }

        private static Dictionary<string, int> CollectCrafts(SessionData session)
        {
            return MergeCounts(session.PlayerStats.Values.Select(stats => stats.CraftedItems));
        }

        private static Dictionary<string, int> MergeCounts(IEnumerable<Dictionary<string, int>> dictionaries)
        {
            Dictionary<string, int> merged = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (Dictionary<string, int> dictionary in dictionaries)
            {
                foreach (KeyValuePair<string, int> pair in dictionary)
                {
                    if (string.IsNullOrWhiteSpace(pair.Key))
                    {
                        continue;
                    }

                    merged.TryGetValue(pair.Key, out int current);
                    merged[pair.Key] = current + Math.Max(1, pair.Value);
                }
            }

            return merged;
        }

        private static void AddItemEvidence(
            IDictionary<ProgressionStage, double> scores,
            ICollection<string> evidence,
            IDictionary<string, int> items,
            double baseWeight,
            string sourceLabel)
        {
            if (items == null)
            {
                return;
            }

            foreach (KeyValuePair<string, int> pair in items)
            {
                StageRule rule = MatchItem(pair.Key);
                if (rule == null)
                {
                    continue;
                }

                double quantityWeight = 1.0 + Math.Min(3.5, Math.Log(Math.Max(1, pair.Value), 2));
                scores[rule.Stage] += baseWeight * quantityWeight;
                AddEvidence(evidence, $"{sourceLabel}: {pair.Key}");
            }
        }

        private static void AddMemoryItemEvidence(
            IDictionary<ProgressionStage, double> scores,
            ICollection<string> evidence,
            WorldMemoryData worldMemory)
        {
            if (worldMemory?.ImportantItems == null)
            {
                return;
            }

            foreach (string item in worldMemory.ImportantItems)
            {
                StageRule rule = MatchItem(item);
                if (rule == null)
                {
                    continue;
                }

                scores[rule.Stage] += 8.0;
                AddEvidence(evidence, $"paměť světa: {item}");
            }
        }

        private static void AddStructureEvidence(
            IDictionary<ProgressionStage, double> scores,
            ICollection<string> evidence,
            IEnumerable<string> structures,
            double weight,
            string sourceLabel)
        {
            if (structures == null)
            {
                return;
            }

            foreach (string structure in structures)
            {
                StageRule rule = MatchStructure(structure);
                if (rule == null)
                {
                    continue;
                }

                scores[rule.Stage] += weight;
                AddEvidence(evidence, $"{sourceLabel}: {structure}");
            }
        }

        private static void AddMemoryStructureEvidence(
            IDictionary<ProgressionStage, double> scores,
            ICollection<string> evidence,
            WorldMemoryData worldMemory)
        {
            if (worldMemory?.ImportantStructures == null)
            {
                return;
            }

            AddStructureEvidence(scores, evidence, worldMemory.ImportantStructures.Select(entry => entry.StructureName), 15.0, "známá stanice");
        }

        private static void AddBossEvidence(
            IDictionary<ProgressionStage, double> scores,
            ICollection<string> evidence,
            IEnumerable<string> bosses,
            double weight,
            string sourceLabel)
        {
            if (bosses == null)
            {
                return;
            }

            foreach (string boss in bosses)
            {
                StageRule rule = MatchBoss(boss);
                if (rule == null)
                {
                    continue;
                }

                scores[rule.Stage] += weight;
                AddEvidence(evidence, $"{sourceLabel}: {boss}");
            }
        }

        private static void AddBiomeEvidence(
            IDictionary<ProgressionStage, double> scores,
            ICollection<string> evidence,
            IEnumerable<string> biomes,
            double weight,
            string sourceLabel)
        {
            if (biomes == null)
            {
                return;
            }

            foreach (string biome in biomes.Where(ChronicleFilters.IsValidBiome))
            {
                StageRule rule = MatchBiome(biome);
                if (rule == null)
                {
                    continue;
                }

                scores[rule.Stage] += weight;
                AddEvidence(evidence, $"{sourceLabel}: {biome}");
            }
        }

        private static ProgressionContext BuildContext(Dictionary<ProgressionStage, double> scores, List<string> evidence)
        {
            double total = scores.Values.Sum();
            if (total <= 0)
            {
                return new ProgressionContext
                {
                    DominantStage = ProgressionStage.EarlyGame,
                    DominantLabel = "Early Game",
                    HasStrongEvidence = false,
                    Confidences = new List<ProgressionConfidence>
                    {
                        new ProgressionConfidence
                        {
                            Stage = ProgressionStage.EarlyGame,
                            Label = "Early Game",
                            Score = 1,
                            Percentage = 100
                        }
                    }
                };
            }

            List<ProgressionConfidence> confidences = scores
                .Where(pair => pair.Value > 0)
                .Select(pair => new ProgressionConfidence
                {
                    Stage = pair.Key,
                    Label = GetLabel(pair.Key),
                    Score = pair.Value,
                    Percentage = (int)Math.Round(pair.Value / total * 100.0)
                })
                .OrderByDescending(item => item.Score)
                .ThenByDescending(item => item.Stage)
                .ToList();

            ProgressionConfidence dominant = confidences.First();
            return new ProgressionContext
            {
                DominantStage = dominant.Stage,
                DominantLabel = dominant.Label,
                Confidences = confidences,
                Evidence = evidence.Take(12).ToList(),
                HasStrongEvidence = dominant.Score >= 16.0 || dominant.Percentage >= 45
            };
        }

        private static StageRule MatchItem(string value)
        {
            string normalized = ChronicleFilters.NormalizeKey(value);
            return Rules.FirstOrDefault(rule => rule.ItemTokens.Any(token => normalized.Contains(token)));
        }

        private static StageRule MatchStructure(string value)
        {
            string normalized = ChronicleFilters.NormalizeKey(value);
            return Rules.FirstOrDefault(rule => rule.StructureTokens.Any(token => normalized.Contains(token)));
        }

        private static StageRule MatchBoss(string value)
        {
            string normalized = ChronicleFilters.NormalizeKey(value);
            return Rules.FirstOrDefault(rule => rule.BossTokens.Any(token => normalized.Contains(token)));
        }

        private static StageRule MatchBiome(string value)
        {
            string normalized = ChronicleFilters.NormalizeKey(value);
            return Rules.FirstOrDefault(rule => rule.BiomeTokens.Any(token => normalized.Contains(token)));
        }

        private static string GetLabel(ProgressionStage stage)
        {
            StageRule rule = Rules.FirstOrDefault(entry => entry.Stage == stage);
            return rule == null ? "Early Game" : rule.Label;
        }

        private static void AddEvidence(ICollection<string> evidence, string value)
        {
            if (evidence.Count >= 20 || string.IsNullOrWhiteSpace(value) || evidence.Contains(value))
            {
                return;
            }

            evidence.Add(value);
        }
    }
}

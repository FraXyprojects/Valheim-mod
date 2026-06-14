using System;
using System.Collections.Generic;
using System.Linq;
using ValheimSessionChronicle.Core;
using ValheimSessionChronicle.Models;
using ValheimSessionChronicle.Utility;
using ValheimSessionChronicle.WorldMemory;

namespace ValheimSessionChronicle.Reporting.Analysis
{
    public sealed class DiscoveryValueAnalyzer
    {
        private sealed class ResourceRule
        {
            public string[] Tokens { get; set; } = new string[0];
            public int MediumQuantity { get; set; }
            public int HighQuantity { get; set; }
            public string OperationType { get; set; } = string.Empty;
            public ProgressionStage Stage { get; set; }
            public string Summary { get; set; } = string.Empty;
        }

        private static readonly ResourceRule[] ResourceRules =
        {
            new ResourceRule
            {
                Tokens = new[] { "wood", "drevo", "finewood", "corewood" },
                MediumQuantity = 120,
                HighQuantity = 300,
                OperationType = "Dřevařská operace",
                Stage = ProgressionStage.BlackForest,
                Summary = "proběhla rozsáhlejší těžba dřeva pro další stavbu a zásobování"
            },
            new ResourceRule
            {
                Tokens = new[] { "copper", "tin", "bronze", "bronz" },
                MediumQuantity = 35,
                HighQuantity = 90,
                OperationType = "Bronzová příprava",
                Stage = ProgressionStage.BlackForest,
                Summary = "výprava pracovala s větším objemem bronzové nebo rudné výbavy"
            },
            new ResourceRule
            {
                Tokens = new[] { "iron", "zelezo" },
                MediumQuantity = 30,
                HighQuantity = 100,
                OperationType = "Těžba železa",
                Stage = ProgressionStage.Swamp,
                Summary = "session měla charakter výrazné železné těžební operace"
            },
            new ResourceRule
            {
                Tokens = new[] { "silver", "stribro" },
                MediumQuantity = 25,
                HighQuantity = 80,
                OperationType = "Horská těžba",
                Stage = ProgressionStage.Mountain,
                Summary = "skupina pracovala s významným objemem horských stříbrných zdrojů"
            },
            new ResourceRule
            {
                Tokens = new[] { "blackmetal", "cernykov" },
                MediumQuantity = 25,
                HighQuantity = 80,
                OperationType = "Plains zpracování kovu",
                Stage = ProgressionStage.Plains,
                Summary = "do popředí se dostaly zásoby černého kovu z Plains"
            },
            new ResourceRule
            {
                Tokens = new[] { "yggdrasil", "sap", "blackcore", "refinedeitr", "eitr", "carapace", "softtissue" },
                MediumQuantity = 10,
                HighQuantity = 40,
                OperationType = "Mistlands zdroje",
                Stage = ProgressionStage.Mistlands,
                Summary = "výprava se dotkla vzácných Mistlands surovin a pozdní magie"
            },
            new ResourceRule
            {
                Tokens = new[] { "flametal", "ashwood", "asksvin", "morgen", "grausten" },
                MediumQuantity = 10,
                HighQuantity = 35,
                OperationType = "Ashlands zdroje",
                Stage = ProgressionStage.Ashlands,
                Summary = "pozorování ukázala práci se surovinami z Ashlands"
            }
        };

        public DiscoveryAnalysis Analyze(
            SessionData session,
            WorldMemoryData worldMemory,
            WorldMemoryUpdateResult memoryUpdate,
            ProgressionContext progression)
        {
            DiscoveryAnalysis analysis = new DiscoveryAnalysis();
            Dictionary<string, int> activeItems = MergeCounts(
                CollectPickups(session),
                CollectCrafts(session));
            Dictionary<string, int> observedItems = MergeMax(
                session.ObservedInventoryItems,
                session.ObservedContainerItems);

            AddDiscoveries(analysis, activeItems, memoryUpdate, progression);
            AddBiomeDiscoveries(analysis, session, memoryUpdate, progression);
            AddStructureDiscoveries(analysis, memoryUpdate, progression);
            AddResourceOperations(analysis, activeItems, "získané suroviny");
            AddResourceOperations(analysis, observedItems, "pozorované zásoby");

            analysis.Discoveries = analysis.Discoveries
                .OrderByDescending(item => item.Tier)
                .ThenByDescending(item => item.Quantity)
                .Take(8)
                .ToList();

            analysis.ResourceOperations = analysis.ResourceOperations
                .OrderByDescending(item => item.Quantity)
                .Take(6)
                .ToList();

            return analysis;
        }

        private static Dictionary<string, int> CollectPickups(SessionData session)
        {
            return MergeCounts(session.PlayerStats.Values.Select(stats => stats.ItemPickups).ToArray());
        }

        private static Dictionary<string, int> CollectCrafts(SessionData session)
        {
            return MergeCounts(session.PlayerStats.Values.Select(stats => stats.CraftedItems).ToArray());
        }

        private static void AddDiscoveries(
            DiscoveryAnalysis analysis,
            IDictionary<string, int> activeItems,
            WorldMemoryUpdateResult memoryUpdate,
            ProgressionContext progression)
        {
            foreach (KeyValuePair<string, int> pair in activeItems)
            {
                if (!ValheimNames.IsImportantItem(pair.Key) && !ValheimNames.IsImportantCraftingMilestone(pair.Key))
                {
                    continue;
                }

                bool known = memoryUpdate != null && memoryUpdate.PreviouslyKnownImportantItems.Contains(pair.Key);
                DiscoveryValueTier tier = DetermineDiscoveryTier(pair.Key, pair.Value, known, progression);
                if (tier < DiscoveryValueTier.Medium)
                {
                    continue;
                }

                analysis.Discoveries.Add(new DiscoveryValueRecord
                {
                    Name = pair.Key,
                    Kind = "item",
                    Quantity = pair.Value,
                    WasKnownBefore = known,
                    Tier = tier,
                    Summary = BuildDiscoverySummary(pair.Key, pair.Value, known, tier)
                });
            }
        }

        private static void AddResourceOperations(DiscoveryAnalysis analysis, IDictionary<string, int> items, string source)
        {
            foreach (KeyValuePair<string, int> pair in items)
            {
                ResourceRule rule = MatchResource(pair.Key);
                if (rule == null || pair.Value < rule.MediumQuantity)
                {
                    continue;
                }

                string scale = pair.Value >= rule.HighQuantity ? "rozsáhlá" : "výrazná";
                string summary = source == "pozorované zásoby"
                    ? $"v zázemí byly zachyceny {scale} zásoby: {pair.Value}x {pair.Key}"
                    : $"{rule.Summary} ({pair.Value}x {pair.Key})";

                analysis.ResourceOperations.Add(new ResourceOperation
                {
                    ResourceName = pair.Key,
                    Quantity = pair.Value,
                    OperationType = rule.OperationType,
                    Source = source,
                    RelatedStage = rule.Stage,
                    Summary = summary
                });
            }
        }

        private static void AddBiomeDiscoveries(
            DiscoveryAnalysis analysis,
            SessionData session,
            WorldMemoryUpdateResult memoryUpdate,
            ProgressionContext progression)
        {
            foreach (string biome in session.Environment.BiomesVisited.Where(ChronicleFilters.IsValidBiome))
            {
                bool known = memoryUpdate != null && memoryUpdate.PreviouslyKnownBiomes.Contains(biome);
                if (known)
                {
                    continue;
                }

                ProgressionStage stage = InferStageFromBiome(biome);
                DiscoveryValueTier tier = stage >= ProgressionStage.Mistlands || stage > progression.DominantStage
                    ? DiscoveryValueTier.Critical
                    : DiscoveryValueTier.High;

                analysis.Discoveries.Add(new DiscoveryValueRecord
                {
                    Name = biome,
                    Kind = "biome",
                    Quantity = 1,
                    WasKnownBefore = false,
                    Tier = tier,
                    Summary = $"první zaznamenaná návštěva biomu {biome}"
                });
            }
        }

        private static void AddStructureDiscoveries(
            DiscoveryAnalysis analysis,
            WorldMemoryUpdateResult memoryUpdate,
            ProgressionContext progression)
        {
            if (memoryUpdate == null)
            {
                return;
            }

            foreach (var structure in memoryUpdate.NewImportantStructures)
            {
                if (memoryUpdate.PreviouslyKnownImportantStructures.Contains(structure.StructureName))
                {
                    continue;
                }

                ProgressionStage stage = InferStageFromStructure(structure.StructureName);
                DiscoveryValueTier tier = stage >= ProgressionStage.Mistlands || stage > progression.DominantStage
                    ? DiscoveryValueTier.Critical
                    : DiscoveryValueTier.High;

                analysis.Discoveries.Add(new DiscoveryValueRecord
                {
                    Name = structure.StructureName,
                    Kind = "structure",
                    Quantity = 1,
                    WasKnownBefore = false,
                    Tier = tier,
                    Summary = $"nově zaznamenaná progresní stanice: {structure.StructureName}"
                });
            }
        }

        private static DiscoveryValueTier DetermineDiscoveryTier(
            string itemName,
            int quantity,
            bool known,
            ProgressionContext progression)
        {
            if (known)
            {
                return DiscoveryValueTier.Low;
            }

            ProgressionStage itemStage = MatchResource(itemName)?.Stage ?? InferStageFromName(itemName);
            if (itemStage >= ProgressionStage.Mistlands || itemStage > progression.DominantStage)
            {
                return DiscoveryValueTier.Critical;
            }

            if (itemStage >= ProgressionStage.Swamp || quantity >= 10)
            {
                return DiscoveryValueTier.High;
            }

            return DiscoveryValueTier.Medium;
        }

        private static string BuildDiscoverySummary(string itemName, int quantity, bool known, DiscoveryValueTier tier)
        {
            if (known)
            {
                return quantity >= 20
                    ? $"známý zdroj {itemName} se objevil ve větším množství ({quantity}x)"
                    : $"opakované získání známého zdroje {itemName}";
            }

            switch (tier)
            {
                case DiscoveryValueTier.Critical:
                    return $"zásadní nový progresní nález: {itemName}";
                case DiscoveryValueTier.High:
                    return $"důležitý nový nález: {itemName}";
                default:
                    return $"nově zaznamenaný objev: {itemName}";
            }
        }

        private static Dictionary<string, int> MergeCounts(params Dictionary<string, int>[] dictionaries)
        {
            Dictionary<string, int> merged = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (Dictionary<string, int> dictionary in dictionaries.Where(dictionary => dictionary != null))
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

        private static Dictionary<string, int> MergeMax(params Dictionary<string, int>[] dictionaries)
        {
            Dictionary<string, int> merged = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (Dictionary<string, int> dictionary in dictionaries.Where(dictionary => dictionary != null))
            {
                foreach (KeyValuePair<string, int> pair in dictionary)
                {
                    if (string.IsNullOrWhiteSpace(pair.Key))
                    {
                        continue;
                    }

                    merged.TryGetValue(pair.Key, out int current);
                    merged[pair.Key] = Math.Max(current, pair.Value);
                }
            }

            return merged;
        }

        private static ResourceRule MatchResource(string itemName)
        {
            string normalized = ChronicleFilters.NormalizeKey(itemName);
            return ResourceRules.FirstOrDefault(rule => rule.Tokens.Any(token => normalized.Contains(token)));
        }

        private static ProgressionStage InferStageFromName(string itemName)
        {
            ResourceRule rule = MatchResource(itemName);
            if (rule != null)
            {
                return rule.Stage;
            }

            string normalized = ChronicleFilters.NormalizeKey(itemName);
            if (normalized.Contains("queen") || normalized.Contains("eitr") || normalized.Contains("blackcore"))
            {
                return ProgressionStage.Mistlands;
            }

            if (normalized.Contains("gjall") || normalized.Contains("yggdrasil") || normalized.Contains("carapace"))
            {
                return ProgressionStage.Mistlands;
            }

            if (normalized.Contains("trophy") || normalized.Contains("trofej"))
            {
                return ProgressionStage.Swamp;
            }

            return ProgressionStage.BlackForest;
        }

        private static ProgressionStage InferStageFromBiome(string biome)
        {
            string normalized = ChronicleFilters.NormalizeKey(biome);
            if (normalized.Contains("ashlands"))
            {
                return ProgressionStage.Ashlands;
            }

            if (normalized.Contains("mistlands"))
            {
                return ProgressionStage.Mistlands;
            }

            if (normalized.Contains("plains"))
            {
                return ProgressionStage.Plains;
            }

            if (normalized.Contains("mountain"))
            {
                return ProgressionStage.Mountain;
            }

            if (normalized.Contains("swamp"))
            {
                return ProgressionStage.Swamp;
            }

            if (normalized.Contains("blackforest"))
            {
                return ProgressionStage.BlackForest;
            }

            return ProgressionStage.EarlyGame;
        }

        private static ProgressionStage InferStageFromStructure(string structureName)
        {
            string normalized = ChronicleFilters.NormalizeKey(structureName);
            if (normalized.Contains("blackforge") || normalized.Contains("galdr") || normalized.Contains("eitrrefinery"))
            {
                return ProgressionStage.Mistlands;
            }

            if (normalized.Contains("blastfurnace") || normalized.Contains("windmill") || normalized.Contains("spinningwheel"))
            {
                return ProgressionStage.Plains;
            }

            if (normalized.Contains("artisan"))
            {
                return ProgressionStage.Mountain;
            }

            if (normalized.Contains("stonecutter"))
            {
                return ProgressionStage.Swamp;
            }

            if (normalized.Contains("forge") || normalized.Contains("kovarna"))
            {
                return ProgressionStage.BlackForest;
            }

            return ProgressionStage.EarlyGame;
        }
    }
}

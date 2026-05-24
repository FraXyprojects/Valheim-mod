using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ValheimSessionChronicle.Core;
using ValheimSessionChronicle.Models;

namespace ValheimSessionChronicle.Utility
{
    internal static class ChronicleFilters
    {
        // Central rules for deciding what belongs in the human chronicle.
        // Low-value telemetry can still feed statistics, but it should not become prose noise.
        private static readonly HashSet<string> InvalidBiomes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            string.Empty,
            "none",
            "unknown",
            "nobiome",
            "no biome"
        };

        private static readonly HashSet<string> CommonResources = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "stone",
            "kamen",
            "kámen",
            "wood",
            "drevo",
            "dřevo",
            "resin",
            "pryskyrice",
            "pryskyřice",
            "mushroom",
            "mushrooms",
            "houba",
            "houby",
            "necktail",
            "neck tail",
            "boarmeat",
            "boar meat",
            "leatherscraps",
            "leather scraps",
            "raspberry",
            "raspberries",
            "malina"
        };

        private static readonly string[] CampPieces =
        {
            "workbench",
            "pracovni stul",
            "pracovní stůl",
            "campfire",
            "ohen",
            "oheň",
            "bed",
            "postel"
        };

        private static readonly string[] DangerousEnemies =
        {
            "troll",
            "abomination",
            "golem",
            "stone golem",
            "gjall",
            "soldier",
            "seeker soldier",
            "berserker",
            "lox",
            "fenring",
            "wraith"
        };

        public static bool IsValidBiome(string biome)
        {
            return !InvalidBiomes.Contains(Normalize(biome));
        }

        public static bool IsCommonResource(string itemName)
        {
            string normalized = NormalizeKey(itemName);
            string bareItemName = normalized.StartsWith("item", StringComparison.Ordinal)
                ? normalized.Substring(4)
                : normalized;

            return CommonResources.Contains(normalized) || CommonResources.Contains(bareItemName);
        }

        public static bool IsCampPiece(string pieceName)
        {
            string normalized = NormalizeKey(pieceName);
            return CampPieces.Any(piece => normalized.Contains(Normalize(piece)));
        }

        public static bool IsFirePiece(string pieceName)
        {
            string normalized = NormalizeKey(pieceName);
            return normalized.Contains("campfire") || normalized.Contains("bonfire") ||
                   normalized.Contains("hearth") || normalized.Contains("fire") ||
                   normalized.Contains("ohen") || normalized.Contains("taborak");
        }

        public static bool IsWorkbenchPiece(string pieceName)
        {
            string normalized = NormalizeKey(pieceName);
            return normalized.Contains("workbench") || normalized.Contains("pracovnistul");
        }

        public static bool IsBedPiece(string pieceName)
        {
            string normalized = NormalizeKey(pieceName);
            return normalized.Contains("bed") || normalized.Contains("postel");
        }

        public static bool IsStoragePiece(string pieceName)
        {
            string normalized = NormalizeKey(pieceName);
            return normalized.Contains("chest") || normalized.Contains("truhla") ||
                   normalized.Contains("cart") || normalized.Contains("storage");
        }

        public static bool IsWallOrDefensePiece(string pieceName)
        {
            string normalized = NormalizeKey(pieceName);
            return normalized.Contains("wall") || normalized.Contains("stake") ||
                   normalized.Contains("palisade") || normalized.Contains("gate") ||
                   normalized.Contains("fence") || normalized.Contains("spike") ||
                   normalized.Contains("hradba") || normalized.Contains("brana");
        }

        public static bool IsPortalPiece(string pieceName)
        {
            return NormalizeKey(pieceName).Contains("portal");
        }

        public static bool IsForgePiece(string pieceName)
        {
            string normalized = NormalizeKey(pieceName);
            return normalized.Contains("forge") || normalized.Contains("kovarna");
        }

        public static bool IsAdvancedStationPiece(string pieceName)
        {
            string normalized = NormalizeKey(pieceName);
            return normalized.Contains("stonecutter") || normalized.Contains("artisan") ||
                   normalized.Contains("blackforge") || normalized.Contains("galdr") ||
                   normalized.Contains("eitr") || normalized.Contains("blastfurnace") ||
                   normalized.Contains("windmill") || normalized.Contains("spinningwheel");
        }

        public static bool IsDangerousEnemy(string enemyName)
        {
            string normalized = NormalizeKey(enemyName);
            return DangerousEnemies.Any(enemy => normalized.Contains(Normalize(enemy)));
        }

        public static bool ShouldAppearInChronicle(SessionEvent entry)
        {
            if (entry == null || entry.Importance < EventImportance.Medium)
            {
                return false;
            }

            if (entry.Type == EventTypes.CombatMoment)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(entry.Biome) && !IsValidBiome(entry.Biome))
            {
                return false;
            }

            if ((entry.Type == EventTypes.ItemPickedUp || entry.Type == EventTypes.Discovery) &&
                IsCommonResource(entry.Target))
            {
                return false;
            }

            return true;
        }

        public static bool TryGetBuildingMilestone(
            string pieceName,
            string biome,
            bool firstPlacement,
            bool firstCampInBiome,
            out string description,
            out EventImportance importance)
        {
            description = string.Empty;
            importance = EventImportance.Medium;

            string normalized = NormalizeKey(pieceName);
            string readableBiome = IsValidBiome(biome) ? biome : "neznámé oblasti";

            // Camps and workstations are reported only as milestones, not as every placement action.
            if (firstCampInBiome && CampPieces.Any(piece => normalized.Contains(Normalize(piece))))
            {
                description = $"V biomu {readableBiome} vznikl malý tábor pro další výpravu.";
                importance = EventImportance.High;
                return true;
            }

            if (!firstPlacement)
            {
                return false;
            }

            if (normalized.Contains("raft") || normalized.Contains("vor"))
            {
                description = "Byl postaven první vor pro průzkum po vodě.";
                importance = EventImportance.High;
                return true;
            }

            if (normalized.Contains("portal"))
            {
                description = "Byl připraven první portál pro rychlý návrat z výpravy.";
                importance = EventImportance.High;
                return true;
            }

            if (normalized.Contains("forge") || normalized.Contains("kovarna") || normalized.Contains("kovárna"))
            {
                description = "Byla založena kovárna a výprava se posunula ke kovové výbavě.";
                importance = EventImportance.High;
                return true;
            }

            if (normalized.Contains("stonecutter") || normalized.Contains("kamenik") || normalized.Contains("kameník"))
            {
                description = "Stavba se posunula ke kamenným konstrukcím díky stonecutteru.";
                importance = EventImportance.Medium;
                return true;
            }

            if (normalized.Contains("artisan") || normalized.Contains("blackforge") || normalized.Contains("black forge") ||
                normalized.Contains("galdr") || normalized.Contains("eitr") || normalized.Contains("blastfurnace") ||
                normalized.Contains("blast furnace"))
            {
                description = $"Byla vybudována důležitá řemeslná stanice: {pieceName}.";
                importance = EventImportance.High;
                return true;
            }

            return false;
        }

        public static string NormalizeKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string formD = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            StringBuilder builder = new StringBuilder(formD.Length);
            foreach (char c in formD)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (category != UnicodeCategory.NonSpacingMark && !char.IsWhiteSpace(c) && c != '_' && c != '-')
                {
                    builder.Append(c);
                }
            }

            return builder.ToString().Normalize(NormalizationForm.FormC);
        }

        private static string Normalize(string value)
        {
            return NormalizeKey(value);
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using ValheimSessionChronicle.Core;

namespace ValheimSessionChronicle.Utility
{
    internal static class ValheimNames
    {
        private static readonly string[] ImportantItems =
        {
            "Queen Bee", "Včelí královna", "Honey", "Med", "Ancient Seed", "Prastaré semeno",
            "Surtling Core", "Surtlingské jádro", "Bronze", "Bronz", "Iron", "Železo", "Silver", "Stříbro",
            "Black metal", "Blackmetal", "Černý kov", "Flametal", "Dragon egg", "Dračí vejce", "Wishbone", "Crypt key",
            "Swamp key", "Yagluth thing", "Sealbreaker", "Dvergr extractor", "Queen drop",
            "Soft tissue", "Black core", "Eitr", "Carapace", "Mandible", "Sap", "Thunder stone",
            "Trophy", "Troféj", "trofej"
        };

        private static readonly string[] ImportantPieces =
        {
            "Workbench", "Forge", "Stonecutter", "Artisan table", "Black forge", "Galdr table",
            "Eitr refinery", "Blast furnace", "Spinning wheel", "Windmill", "Cauldron"
        };

        private static readonly Dictionary<string, string> BossNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Eikthyr"] = "Eikthyr",
            ["gd_king"] = "The Elder",
            ["The Elder"] = "The Elder",
            ["Bonemass"] = "Bonemass",
            ["Dragon"] = "Moder",
            ["Moder"] = "Moder",
            ["GoblinKing"] = "Yagluth",
            ["Yagluth"] = "Yagluth",
            ["SeekerQueen"] = "The Queen",
            ["The Queen"] = "The Queen",
            ["Fader"] = "Fader"
        };

        public static string GetLocalPlayerName()
        {
            return GetPlayerName(Player.m_localPlayer);
        }

        public static string GetPlayerName(Player player)
        {
            if (player == null)
            {
                return "Neznámý hráč";
            }

            object methodName = SafeInvoke(player, "GetPlayerName");
            if (methodName is string name && !string.IsNullOrWhiteSpace(name))
            {
                return Clean(Localize(name));
            }

            return GetCharacterName(player);
        }

        public static string GetCharacterName(Character character)
        {
            if (character == null)
            {
                return "Neznámý tvor";
            }

            object hoverName = SafeInvoke(character, "GetHoverName");
            if (hoverName is string hover && !string.IsNullOrWhiteSpace(hover))
            {
                return Clean(Localize(hover));
            }

            string rawName = ValheimReflection.GetMemberValue<string>(character, "m_name");
            if (!string.IsNullOrWhiteSpace(rawName))
            {
                return Clean(Localize(rawName));
            }

            return Clean(character.name);
        }

        public static bool IsPlayer(Character character)
        {
            if (character is Player)
            {
                return true;
            }

            object isPlayer = SafeInvoke(character, "IsPlayer");
            return isPlayer is bool value && value;
        }

        public static bool IsLocalPlayer(Player player)
        {
            return player != null && Player.m_localPlayer != null && player == Player.m_localPlayer;
        }

        public static bool IsBoss(Character character)
        {
            if (character == null)
            {
                return false;
            }

            if (ValheimReflection.TryGetBool(character, "m_boss", out bool bossFlag) && bossFlag)
            {
                return true;
            }

            object isBoss = SafeInvoke(character, "IsBoss");
            if (isBoss is bool value && value)
            {
                return true;
            }

            string name = character.name ?? string.Empty;
            return BossNames.Keys.Any(key => name.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public static string NormalizeBossName(string bossName)
        {
            bossName = Clean(bossName);
            foreach (KeyValuePair<string, string> pair in BossNames)
            {
                if (bossName.IndexOf(pair.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return pair.Value;
                }
            }

            return bossName;
        }

        public static bool ShouldIgnoreEnemy(Character character)
        {
            if (character == null || IsPlayer(character))
            {
                return true;
            }

            if (ValheimReflection.TryGetBool(character, "m_tamed", out bool tamed) && tamed)
            {
                return true;
            }

            string name = GetCharacterName(character);
            return string.IsNullOrWhiteSpace(name) ||
                   name.Equals("Neznámý tvor", StringComparison.OrdinalIgnoreCase);
        }

        public static Character GetHitAttacker(object hitData)
        {
            object attacker = SafeInvoke(hitData, "GetAttacker");
            return attacker as Character;
        }

        public static float GetCharacterHealth(Character character)
        {
            if (character == null)
            {
                return 0f;
            }

            object health = SafeInvoke(character, "GetHealth");
            if (health is float healthValue)
            {
                return healthValue;
            }

            return ValheimReflection.GetMemberValue<float>(character, "m_health");
        }

        public static float GetCharacterMaxHealth(Character character)
        {
            if (character == null)
            {
                return 0f;
            }

            object maxHealth = SafeInvoke(character, "GetMaxHealth");
            if (maxHealth is float maxHealthValue)
            {
                return maxHealthValue;
            }

            return ValheimReflection.GetMemberValue<float>(character, "m_maxHealth");
        }

        public static float GetHitTotalDamage(object hitData)
        {
            if (hitData == null)
            {
                return 0f;
            }

            object total = SafeInvoke(hitData, "GetTotalDamage");
            if (total is float totalValue)
            {
                return totalValue;
            }

            object damage = ValheimReflection.GetMemberValue(hitData, "m_damage");
            object damageTotal = SafeInvoke(damage, "GetTotalDamage");
            return damageTotal is float damageTotalValue ? damageTotalValue : 0f;
        }

        public static string GetCurrentBiomeName(Player player)
        {
            if (player == null)
            {
                return string.Empty;
            }

            object biome = SafeInvoke(player, "GetCurrentBiome");
            if (biome != null)
            {
                return FormatBiomeName(biome.ToString());
            }

            if (EnvMan.instance != null)
            {
                object envBiome = SafeInvoke(EnvMan.instance, "GetCurrentBiome");
                if (envBiome != null)
                {
                    return FormatBiomeName(envBiome.ToString());
                }
            }

            try
            {
                if (WorldGenerator.instance != null)
                {
                    object worldBiome = ValheimReflection.Invoke(WorldGenerator.instance, "GetBiome", player.transform.position);
                    if (worldBiome != null)
                    {
                        return FormatBiomeName(worldBiome.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                ChronicleLogger.Verbose($"Biome fallback failed: {ex.Message}");
            }

            return string.Empty;
        }

        public static string GetCurrentWeatherName()
        {
            try
            {
                if (EnvMan.instance == null)
                {
                    return string.Empty;
                }

                object environment = SafeInvoke(EnvMan.instance, "GetCurrentEnvironment");
                if (environment is string envString)
                {
                    return Clean(envString);
                }

                string current = ValheimReflection.GetMemberValue<string>(EnvMan.instance, "m_currentEnv");
                return Clean(current);
            }
            catch (Exception ex)
            {
                ChronicleLogger.Verbose($"Weather lookup failed: {ex.Message}");
                return string.Empty;
            }
        }

        public static bool? IsNight()
        {
            try
            {
                if (EnvMan.instance == null)
                {
                    return null;
                }

                object isNight = SafeInvoke(EnvMan.instance, "IsNight");
                if (isNight is bool night)
                {
                    return night;
                }
            }
            catch (Exception ex)
            {
                ChronicleLogger.Verbose($"Night lookup failed: {ex.Message}");
            }

            return null;
        }

        public static string GetServerName()
        {
            try
            {
                if (ZNet.instance == null)
                {
                    return "Dedicated Server";
                }

                string[] methodNames = { "GetServerString", "GetServerName", "GetWorldName" };
                foreach (string methodName in methodNames)
                {
                    object value = SafeInvoke(ZNet.instance, methodName);
                    if (value is string text && !string.IsNullOrWhiteSpace(text))
                    {
                        return Clean(text);
                    }
                }

                string fieldName = ValheimReflection.GetMemberValue<string>(ZNet.instance, "m_serverName");
                if (!string.IsNullOrWhiteSpace(fieldName))
                {
                    return Clean(fieldName);
                }
            }
            catch (Exception ex)
            {
                ChronicleLogger.Verbose($"Server name lookup failed: {ex.Message}");
            }

            return "Dedicated Server";
        }

        public static string GetWorldName()
        {
            try
            {
                if (ZNet.instance == null)
                {
                    return string.Empty;
                }

                object value = SafeInvoke(ZNet.instance, "GetWorldName");
                if (value is string text && !string.IsNullOrWhiteSpace(text))
                {
                    return Clean(text);
                }
            }
            catch (Exception ex)
            {
                ChronicleLogger.Verbose($"World name lookup failed: {ex.Message}");
            }

            return string.Empty;
        }

        public static string FormatPosition(Vector3 position)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0:0}, {1:0}, {2:0}",
                position.x,
                position.y,
                position.z);
        }

        public static Player FindPlayerArgument(object[] args)
        {
            if (args == null)
            {
                return null;
            }

            for (int index = 0; index < args.Length; index++)
            {
                if (args[index] is Player player)
                {
                    return player;
                }

                if (args[index] is Humanoid humanoid && humanoid is Player humanoidPlayer)
                {
                    return humanoidPlayer;
                }
            }

            return null;
        }

        public static IEnumerable GetAllKnownPlayers()
        {
            object players = SafeInvoke(typeof(Player), "GetAllPlayers");
            if (players is IEnumerable enumerable)
            {
                return enumerable;
            }

            // Fallback for Valheim builds where the helper method is not available.
            return UnityEngine.Object.FindObjectsByType<Player>(FindObjectsSortMode.None);
        }

        public static object FindPieceArgument(object[] args)
        {
            if (args == null)
            {
                return null;
            }

            foreach (object arg in args)
            {
                if (arg == null)
                {
                    continue;
                }

                if (arg is Piece)
                {
                    return arg;
                }

                if (arg is GameObject gameObject)
                {
                    Piece piece = gameObject.GetComponent<Piece>();
                    if (piece != null)
                    {
                        return piece;
                    }
                }
            }

            return null;
        }

        public static string GetPieceName(object piece)
        {
            if (piece == null)
            {
                return string.Empty;
            }

            string name = ValheimReflection.GetMemberValue<string>(piece, "m_name");
            if (!string.IsNullOrWhiteSpace(name))
            {
                return Clean(Localize(name));
            }

            if (piece is Component component)
            {
                return Clean(component.name);
            }

            return Clean(piece.ToString());
        }

        public static bool IsWorkstationPiece(object piece, string pieceName)
        {
            if (piece is Component component && component.GetComponent<CraftingStation>() != null)
            {
                return true;
            }

            return ImportantPieces.Any(value => pieceName.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public static bool IsImportantPiece(string pieceName)
        {
            return ImportantPieces.Any(value => pieceName.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public static string ExtractRecipeName(object[] args)
        {
            if (args == null)
            {
                return string.Empty;
            }

            foreach (object arg in args)
            {
                if (arg == null)
                {
                    continue;
                }

                if (arg.GetType().Name == "Recipe")
                {
                    object item = ValheimReflection.GetMemberValue(arg, "m_item");
                    string itemName = GetItemDropName(item);
                    if (!string.IsNullOrWhiteSpace(itemName))
                    {
                        return itemName;
                    }

                    string recipeName = ValheimReflection.GetMemberValue<string>(arg, "m_name");
                    if (!string.IsNullOrWhiteSpace(recipeName))
                    {
                        return Clean(Localize(recipeName));
                    }
                }
            }

            return string.Empty;
        }

        public static string ExtractItemName(object[] args)
        {
            if (args == null)
            {
                return string.Empty;
            }

            foreach (object arg in args)
            {
                string name = GetItemDropName(arg);
                if (!string.IsNullOrWhiteSpace(name))
                {
                    return name;
                }

                if (arg is GameObject gameObject)
                {
                    ItemDrop itemDrop = gameObject.GetComponent<ItemDrop>();
                    name = GetItemDropName(itemDrop);
                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        return name;
                    }
                }
            }

            return string.Empty;
        }

        public static Dictionary<string, int> GetInventoryItemCounts(object inventoryOwnerOrInventory)
        {
            Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (inventoryOwnerOrInventory == null)
            {
                return counts;
            }

            object inventory = ValheimReflection.GetMemberValue(inventoryOwnerOrInventory, "m_inventory") ?? inventoryOwnerOrInventory;
            object items = SafeInvoke(inventory, "GetAllItems");
            if (!(items is IEnumerable enumerable))
            {
                return counts;
            }

            foreach (object itemData in enumerable)
            {
                string itemName = GetItemDataName(itemData);
                if (!ShouldKeepProgressionItemObservation(itemName))
                {
                    continue;
                }

                int stack = ValheimReflection.GetMemberValue<int>(itemData, "m_stack", 1);
                counts.TryGetValue(itemName, out int current);
                counts[itemName] = current + Math.Max(1, stack);
            }

            return counts;
        }

        public static string GetPortalTag(object portal)
        {
            if (portal == null)
            {
                return string.Empty;
            }

            object tag = SafeInvoke(portal, "GetText");
            if (tag is string tagText && !string.IsNullOrWhiteSpace(tagText))
            {
                return Clean(tagText);
            }

            string field = ValheimReflection.GetMemberValue<string>(portal, "m_tag");
            return Clean(field);
        }

        public static bool IsImportantItem(string itemName)
        {
            if (string.IsNullOrWhiteSpace(itemName) || ChronicleFilters.IsCommonResource(itemName))
            {
                return false;
            }

            string normalized = ChronicleFilters.NormalizeKey(itemName);
            string bareItemName = normalized.StartsWith("item", StringComparison.Ordinal)
                ? normalized.Substring(4)
                : normalized;

            return ImportantItems.Any(value =>
            {
                string important = ChronicleFilters.NormalizeKey(value);
                return normalized.Contains(important) || bareItemName.Contains(important);
            });
        }

        public static bool IsImportantCraftingMilestone(string itemName)
        {
            return IsImportantItem(itemName) || ImportantPieces.Any(value => itemName.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool ShouldKeepProgressionItemObservation(string itemName)
        {
            if (string.IsNullOrWhiteSpace(itemName) || ChronicleFilters.IsCommonResource(itemName))
            {
                return false;
            }

            if (IsImportantItem(itemName))
            {
                return true;
            }

            string normalized = ChronicleFilters.NormalizeKey(itemName);
            string[] tokens =
            {
                "finewood", "corewood", "bronze", "bronz", "copper", "tin", "iron", "zelezo",
                "silver", "stribro", "wolf", "blackmetal", "cernykov", "flametal", "ashwood",
                "yggdrasil", "sap", "blackcore", "eitr", "carapace", "softtissue", "asksvin",
                "morgen", "barley", "flax", "lox", "ancientseed", "surtlingcore", "cryptkey",
                "swampkey", "dragonegg", "queenbee", "honey"
            };

            return tokens.Any(token => normalized.Contains(token));
        }

        private static string GetItemDropName(object possibleItem)
        {
            if (possibleItem == null)
            {
                return string.Empty;
            }

            object itemData = ValheimReflection.GetMemberValue(possibleItem, "m_itemData");
            object shared = ValheimReflection.GetMemberValue(itemData, "m_shared");
            string sharedName = ValheimReflection.GetMemberValue<string>(shared, "m_name");
            if (!string.IsNullOrWhiteSpace(sharedName))
            {
                return Clean(Localize(sharedName));
            }

            if (possibleItem is Component component)
            {
                return Clean(component.name);
            }

            return string.Empty;
        }

        private static string GetItemDataName(object itemData)
        {
            if (itemData == null)
            {
                return string.Empty;
            }

            object shared = ValheimReflection.GetMemberValue(itemData, "m_shared");
            string sharedName = ValheimReflection.GetMemberValue<string>(shared, "m_name");
            if (!string.IsNullOrWhiteSpace(sharedName))
            {
                return Clean(Localize(sharedName));
            }

            string directName = ValheimReflection.GetMemberValue<string>(itemData, "m_name");
            return Clean(Localize(directName));
        }

        private static object SafeInvoke(object target, string methodName, params object[] args)
        {
            try
            {
                return ValheimReflection.Invoke(target, methodName, args);
            }
            catch (Exception ex)
            {
                ChronicleLogger.Verbose($"Reflection call failed: {methodName}: {ex.Message}");
                return null;
            }
        }

        private static string Localize(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            try
            {
                if (text.StartsWith("$", StringComparison.Ordinal) && Localization.instance != null)
                {
                    return Localization.instance.Localize(text);
                }
            }
            catch
            {
                // Localization is best effort. Raw tokens are still better than crashing.
            }

            return text;
        }

        private static string FormatBiomeName(string biome)
        {
            if (string.IsNullOrWhiteSpace(biome))
            {
                return string.Empty;
            }

            return biome.Replace("BlackForest", "Black Forest");
        }

        private static string Clean(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value
                .Replace("(Clone)", string.Empty)
                .Replace("$", string.Empty)
                .Replace("_", " ")
                .Trim();
        }
    }
}

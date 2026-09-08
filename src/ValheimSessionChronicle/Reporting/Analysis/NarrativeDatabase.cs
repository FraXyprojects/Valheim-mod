using System;
using ValheimSessionChronicle.Localization;

namespace ValheimSessionChronicle.Reporting.Analysis
{
    public static class NarrativeDatabase
    {
        // 1. Exploration and Biomes
        public static string GetSingleBiomeExploration(string biome)
        {
            return LocalizationManager.GetString("SingleBiomeExploration", biome);
        }

        public static string GetMultiBiomeExploration(string startBiome, string endBiome)
        {
            return LocalizationManager.GetString("MultiBiomeExploration", startBiome, endBiome);
        }

        // 2. Base building and Expansion
        public static string GetCampNew(string newTierName, string biome)
        {
            string biomeSuffix = string.IsNullOrWhiteSpace(biome) ? string.Empty : $" v biomu {biome}";
            return LocalizationManager.GetString("CampNew", newTierName.ToLowerInvariant(), biomeSuffix);
        }

        public static string GetCampUpgrade(string previousTierName, string biome, string newTierName)
        {
            string biomeSuffix = string.IsNullOrWhiteSpace(biome) ? string.Empty : $" v biomu {biome}";
            return LocalizationManager.GetString("CampUpgrade", previousTierName.ToLowerInvariant(), biomeSuffix, newTierName.ToLowerInvariant());
        }

        public static string GetCampAddedAdvancedStation(string biome)
        {
            string biomeSuffix = string.IsNullOrWhiteSpace(biome) ? string.Empty : $" v biomu {biome}";
            return LocalizationManager.GetString("CampAddedAdvancedStation", biomeSuffix);
        }

        public static string GetCampAddedDefenses(string biome)
        {
            string biomeSuffix = string.IsNullOrWhiteSpace(biome) ? string.Empty : $" v biomu {biome}";
            return LocalizationManager.GetString("CampAddedDefenses", biomeSuffix);
        }

        public static string GetCampExpanded(string biome)
        {
            string biomeSuffix = string.IsNullOrWhiteSpace(biome) ? string.Empty : $" v biomu {biome}";
            return LocalizationManager.GetString("CampExpanded", biomeSuffix);
        }

        public static string GetCampGeneric(string name, string biome, int structureCount)
        {
            string biomeSuffix = string.IsNullOrWhiteSpace(biome) ? string.Empty : $" v biomu {biome}";
            return LocalizationManager.GetString("CampGeneric", name.ToLowerInvariant(), biomeSuffix, structureCount.ToString());
        }

        // 3. Combat Intensity
        public static string GetCombatExtreme()
        {
            return LocalizationManager.GetString("CombatExtreme");
        }

        public static string GetCombatHigh()
        {
            return LocalizationManager.GetString("CombatHigh");
        }

        public static string GetCombatMedium()
        {
            return LocalizationManager.GetString("CombatMedium");
        }

        public static string GetCombatLow()
        {
            return LocalizationManager.GetString("CombatLow");
        }

        // 4. Survival & Near Death
        public static string GetSurvivalHeroicEscape(string playerName)
        {
            return LocalizationManager.GetString("SurvivalHeroicEscape", playerName);
        }

        public static string GetSurvivalLastStand(string playerName)
        {
            return LocalizationManager.GetString("SurvivalLastStand", playerName);
        }

        public static string GetSurvivalNearDeath(string playerName, string healthPercent)
        {
            return LocalizationManager.GetString("SurvivalNearDeath", playerName, healthPercent);
        }

        public static string GetSurvivalNoDeathsHighStress()
        {
            return LocalizationManager.GetString("SurvivalNoDeathsHighStress");
        }

        public static string GetSurvivalMediumStress()
        {
            return LocalizationManager.GetString("SurvivalMediumStress");
        }

        public static string GetSurvivalWithDeaths()
        {
            return LocalizationManager.GetString("SurvivalWithDeaths");
        }

        // 5. Boss Encounters
        public static string GetBossNoDeath()
        {
            return LocalizationManager.GetString("BossNoDeath");
        }

        public static string GetBossWithDeath()
        {
            return LocalizationManager.GetString("BossWithDeath");
        }

        // 6. Progression
        public static string GetProgressionPhase(string label)
        {
            return LocalizationManager.GetString("ProgressionPhase", label);
        }

        // 7. Resource Operations
        public static string GetResourceOperation(string operation)
        {
            return LocalizationManager.GetString("ResourceOperationPhase", operation);
        }
    }
}

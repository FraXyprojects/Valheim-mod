using System;
using ValheimSessionChronicle.Models;
using ValheimSessionChronicle.Utility;

namespace ValheimSessionChronicle.Core
{
    public static class EventSanitizer
    {
        public static bool IsValid(
            string type,
            string category,
            string description,
            string actor,
            string target,
            string biome,
            double sessionSeconds)
        {
            if (sessionSeconds < 0)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(biome) && !ChronicleFilters.IsValidBiome(biome))
            {
                return false;
            }

            if (type == EventTypes.EnemyKilled || type == EventTypes.BossKilled)
            {
                if (string.IsNullOrWhiteSpace(target))
                {
                    return false;
                }
            }

            return true;
        }
    }
}

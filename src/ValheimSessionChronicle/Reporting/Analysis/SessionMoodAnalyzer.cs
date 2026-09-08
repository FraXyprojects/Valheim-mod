using System;
using System.Linq;
using ValheimSessionChronicle.Models;

namespace ValheimSessionChronicle.Reporting.Analysis
{
    public static class SessionMoodAnalyzer
    {
        public static string AnalyzeMood(SessionData session, CombatIntensityResult combatIntensity)
        {
            int totalDeaths = session.PlayerStats.Values.Sum(s => s.Deaths);
            int dangerousEncounters = session.PlayerStats.Values.Sum(s => s.DangerousEncounters);

            if (totalDeaths > 2 && combatIntensity.Tier >= CombatIntensityTier.High)
                return "Mood_Desperate";

            if (combatIntensity.Tier == CombatIntensityTier.Extreme)
                return "Mood_Brutal";

            if (session.Events.Any(e => e.Type == EventTypes.BossKilled) && totalDeaths == 0)
                return "Mood_Triumphant";

            if (combatIntensity.Tier >= CombatIntensityTier.Medium && dangerousEncounters > 0)
                return "Mood_Tense";

            if (session.Events.Count(e => e.Category == EventCategories.Building) > 20)
                return "Mood_Building";

            if (session.Events.Count(e => e.Type == EventTypes.BiomeEntered || e.Type == EventTypes.Discovery) > 5)
                return "Mood_Exploration";

            return "Mood_Calm";
        }
    }
}

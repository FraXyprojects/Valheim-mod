using System;
using System.Collections.Generic;
using System.Linq;
using ValheimSessionChronicle.Models;

namespace ValheimSessionChronicle.Reporting.Analysis
{
    public static class SessionPhaseAnalyzer
    {
        public static List<string> AnalyzePhases(SessionData session)
        {
            var phases = new List<string>();
            var events = session.Events.OrderBy(e => e.TimestampUtc).ToList();

            if (events.Count == 0 || session.Duration.TotalMinutes < 10)
            {
                return phases; // Too short for meaningful phases
            }

            phases.Add("Phase_Arrival");

            bool hasExploration = events.Any(e => e.Type == EventTypes.BiomeEntered || e.Type == EventTypes.Discovery);
            if (hasExploration) phases.Add("Phase_Exploration");

            bool hasBuilding = session.Events.Count(e => e.Category == EventCategories.Building) > 5;
            if (hasBuilding) phases.Add("Phase_Building");

            int combatCount = session.Events.Count(e => e.Category == EventCategories.Combat);
            if (combatCount > 20) phases.Add("Phase_IntenseCombat");
            else if (combatCount > 5) phases.Add("Phase_Combat");

            bool hasBoss = session.Events.Any(e => e.Type == EventTypes.BossKilled);
            if (hasBoss) phases.Add("Phase_BossFight");

            phases.Add("Phase_Conclusion");

            return phases.Distinct().ToList();
        }
    }
}

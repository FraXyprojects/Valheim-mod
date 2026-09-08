using System;
using System.Collections.Generic;
using System.Linq;
using ValheimSessionChronicle.Models;

namespace ValheimSessionChronicle.Reporting.Analysis
{
    public sealed class NarrativePacingAnalyzer
    {
        private const int WindowSizeMinutes = 2;

        public NarrativePacingResult Analyze(SessionData session)
        {
            var result = new NarrativePacingResult();
            var events = session.Events.OrderBy(e => e.TimestampUtc).ToList();
            if (events.Count == 0) return result;

            var startTime = session.StartTimeUtc;
            var endTime = events.Last().TimestampUtc;

            int numWindows = (int)Math.Ceiling((endTime - startTime).TotalMinutes / WindowSizeMinutes) + 1;

            var windows = new List<PacingWindow>();
            for (int i = 0; i < numWindows; i++)
            {
                windows.Add(new PacingWindow
                {
                    StartTime = startTime.AddMinutes(i * WindowSizeMinutes),
                    EndTime = startTime.AddMinutes((i + 1) * WindowSizeMinutes)
                });
            }

            foreach (var evt in events)
            {
                int windowIndex = (int)Math.Floor((evt.TimestampUtc - startTime).TotalMinutes / WindowSizeMinutes);
                if (windowIndex >= 0 && windowIndex < windows.Count)
                {
                    windows[windowIndex].Events.Add(evt);
                }
            }

            foreach (var window in windows)
            {
                window.State = DetermineState(window.Events);
                result.Windows.Add(window);
            }

            return result;
        }

        private string DetermineState(List<SessionEvent> events)
        {
            if (events.Count == 0) return "Pacing_Calm";

            int combatCount = events.Count(e => e.Category == EventCategories.Combat);
            int buildCount = events.Count(e => e.Category == EventCategories.Building);
            int exploreCount = events.Count(e => e.Type == EventTypes.BiomeEntered || e.Type == EventTypes.Discovery);

            if (events.Any(e => e.Type == EventTypes.BossKilled)) return "Pacing_BossCombat";
            if (combatCount >= 10 || events.Any(e => e.Category == EventCategories.Combat && e.Type == EventTypes.Death)) return "Pacing_CombatPeak";
            if (combatCount >= 4) return "Pacing_Combat";
            if (buildCount >= 5) return "Pacing_Building";
            if (exploreCount >= 2) return "Pacing_Exploration";

            return "Pacing_Activity";
        }
    }

    public class NarrativePacingResult
    {
        public List<PacingWindow> Windows { get; set; } = new List<PacingWindow>();
    }

    public class PacingWindow
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<SessionEvent> Events { get; set; } = new List<SessionEvent>();
        public string State { get; set; }
    }
}

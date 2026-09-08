using System;
using System.Collections.Generic;
using ValheimSessionChronicle.Models;

namespace ValheimSessionChronicle.Reporting.Analysis
{
    public enum PacingPhaseType
    {
        Calm,
        Preparation,
        Exploration,
        Activity,
        Building,
        Combat,
        CombatPeak,
        BossCombat,
        Travel,
        Conclusion
    }

    public class PacingPhase
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public PacingPhaseType PhaseType { get; set; }
        public double Intensity { get; set; }
        public int EventCount { get; set; }
        public bool IsPeak { get; set; }
        public bool IsValley { get; set; }

        // Context information
        public string DominantBiome { get; set; } = string.Empty;

        // Lists of events for generating specific narratives
        public List<SessionEvent> Events { get; set; } = new List<SessionEvent>();

        // Participants determined by evidence
        public List<string> StrongEvidenceParticipants { get; set; } = new List<string>();
        public List<string> MediumEvidenceParticipants { get; set; } = new List<string>();
        public List<string> WeakEvidenceParticipants { get; set; } = new List<string>();

        public TimeSpan Duration => EndTime - StartTime;
    }
}

using System.Collections.Generic;

namespace ValheimSessionChronicle.Reporting
{
    public sealed class NarrativeContext
    {
        public List<string> Phases { get; set; } = new List<string>();
        public string ExplorationParagraph { get; set; }
        public string ProgressionParagraph { get; set; }
        public string ResourceParagraph { get; set; }
        public string DiscoveryParagraph { get; set; }
        public string PortalNetworkParagraph { get; set; }
        public string ProfileParagraph { get; set; }
        public string CombatParagraph { get; set; }
        public string SurvivalParagraph { get; set; }
        public string EndingParagraph { get; set; }
        public List<string> PacingTimeline { get; set; } = new List<string>();
    }
}

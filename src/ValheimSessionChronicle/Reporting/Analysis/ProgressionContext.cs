using System.Collections.Generic;

namespace ValheimSessionChronicle.Reporting.Analysis
{
    public sealed class ProgressionContext
    {
        public ProgressionStage DominantStage { get; set; } = ProgressionStage.EarlyGame;
        public string DominantLabel { get; set; } = "Early Game";
        public List<ProgressionConfidence> Confidences { get; set; } = new List<ProgressionConfidence>();
        public List<string> Evidence { get; set; } = new List<string>();
        public bool HasStrongEvidence { get; set; }
    }
}

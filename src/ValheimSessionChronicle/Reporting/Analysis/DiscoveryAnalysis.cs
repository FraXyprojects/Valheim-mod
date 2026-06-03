using System.Collections.Generic;

namespace ValheimSessionChronicle.Reporting.Analysis
{
    public sealed class DiscoveryAnalysis
    {
        public List<DiscoveryValueRecord> Discoveries { get; set; } = new List<DiscoveryValueRecord>();
        public List<ResourceOperation> ResourceOperations { get; set; } = new List<ResourceOperation>();

        public bool HasMeaningfulContent
        {
            get { return Discoveries.Count > 0 || ResourceOperations.Count > 0; }
        }
    }
}

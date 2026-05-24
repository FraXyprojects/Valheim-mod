using System.Collections.Generic;
using ValheimSessionChronicle.Models;

namespace ValheimSessionChronicle.Reporting.Analysis
{
    public sealed class CampCluster
    {
        public CampTier Tier { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Biome { get; set; } = string.Empty;
        public int StructureCount { get; set; }
        public int WorkstationCount { get; set; }
        public List<BuildActivitySample> Samples { get; set; } = new List<BuildActivitySample>();
        public float CenterX { get; set; }
        public float CenterZ { get; set; }
    }
}

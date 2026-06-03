using System;

namespace ValheimSessionChronicle.Models
{
    public sealed class ProgressionStructureObservation
    {
        public DateTime TimestampUtc { get; set; }
        public string StructureName { get; set; } = string.Empty;
        public string StructureType { get; set; } = string.Empty;
        public string Biome { get; set; } = string.Empty;
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }
}

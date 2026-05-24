using System;

namespace ValheimSessionChronicle.Models
{
    public sealed class BuildActivitySample
    {
        public DateTime TimestampUtc { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public string PieceName { get; set; } = string.Empty;
        public string Biome { get; set; } = string.Empty;
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public bool IsFire { get; set; }
        public bool IsWorkbench { get; set; }
        public bool IsBed { get; set; }
        public bool IsStorage { get; set; }
        public bool IsWallOrDefense { get; set; }
        public bool IsPortal { get; set; }
        public bool IsForge { get; set; }
        public bool IsAdvancedStation { get; set; }
        public bool IsWorkstation { get; set; }
    }
}

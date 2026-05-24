namespace ValheimSessionChronicle.WorldMemory
{
    public sealed class WorldMemorySessionContext
    {
        public string WorldDirectory { get; set; } = string.Empty;
        public string RawDirectory { get; set; } = string.Empty;
        public string MemoryPath { get; set; } = string.Empty;
        public WorldMemoryData Memory { get; set; }
        public WorldMemoryUpdateResult UpdateResult { get; set; }
    }
}

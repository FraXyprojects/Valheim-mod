using System.IO;
using ValheimSessionChronicle.Models;

namespace ValheimSessionChronicle.WorldMemory
{
    public sealed class WorldMemorySystem
    {
        private readonly WorldMemoryStorage _storage = new WorldMemoryStorage();
        private readonly WorldMemoryManager _manager = new WorldMemoryManager();

        public WorldMemorySessionContext PrepareSessionMemory(SessionData session)
        {
            string worldDirectory = _storage.GetWorldDirectory(session);
            Directory.CreateDirectory(worldDirectory);

            WorldMemoryData memory = _storage.LoadOrCreate(session);
            WorldMemoryUpdateResult updateResult = _manager.UpdateMemory(memory, session);

            return new WorldMemorySessionContext
            {
                WorldDirectory = worldDirectory,
                RawDirectory = _storage.GetRawDirectory(session),
                MemoryPath = _storage.GetMemoryPath(session),
                Memory = memory,
                UpdateResult = updateResult
            };
        }

        public void Save(SessionData session, WorldMemorySessionContext context)
        {
            if (context?.Memory != null)
            {
                _storage.Save(session, context.Memory);
            }
        }
    }
}

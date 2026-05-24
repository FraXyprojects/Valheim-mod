using System;
using System.IO;
using BepInEx;
using Newtonsoft.Json;
using ValheimSessionChronicle.Configuration;
using ValheimSessionChronicle.Models;
using ValheimSessionChronicle.Reporting;
using ValheimSessionChronicle.WorldMemory;

namespace ValheimSessionChronicle.Storage
{
    public sealed class SessionStorage
    {
        private readonly ReportGenerator _reportGenerator;
        private readonly ChronicleFileNameBuilder _fileNameBuilder = new ChronicleFileNameBuilder();
        private readonly WorldMemorySystem _worldMemorySystem = new WorldMemorySystem();

        public SessionStorage(ReportGenerator reportGenerator)
        {
            _reportGenerator = reportGenerator;
        }

        public StorageResult Save(SessionData session, ChronicleConfig config)
        {
            string baseName = _fileNameBuilder.BuildBaseName(session);
            WorldMemorySessionContext memoryContext = _worldMemorySystem.PrepareSessionMemory(session);

            StorageResult result = new StorageResult();

            if (config.SaveTXT.Value)
            {
                result.TxtReport = _reportGenerator.Generate(session, config.IncludeCompactTimeline.Value, memoryContext.Memory, memoryContext.UpdateResult);
                result.TxtPath = Path.Combine(memoryContext.WorldDirectory, baseName + ".txt");
                File.WriteAllText(result.TxtPath, result.TxtReport);
            }

            if (config.EnableDebugJsonExport.Value)
            {
                Directory.CreateDirectory(memoryContext.RawDirectory);
                result.JsonPath = Path.Combine(memoryContext.RawDirectory, baseName + "_session.debug.json");
                string json = JsonConvert.SerializeObject(session, Formatting.Indented);
                File.WriteAllText(result.JsonPath, json);
            }

            _worldMemorySystem.Save(session, memoryContext);
            result.WorldMemoryPath = memoryContext.MemoryPath;

            return result;
        }

        public static string GetReportsDirectory()
        {
            return Path.Combine(Paths.PluginPath, "ValheimSessionChronicle", "Reports");
        }
    }
}

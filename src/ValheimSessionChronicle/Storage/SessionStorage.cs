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

            if (config.SaveMarkdown.Value)
            {
                // Basic Markdown support using the same generator for now but with md extension
                string mdPath = Path.Combine(memoryContext.WorldDirectory, baseName + ".md");
                File.WriteAllText(mdPath, result.TxtReport);
            }

            if (config.SaveJSON.Value)
            {
                string cleanJsonPath = Path.Combine(memoryContext.WorldDirectory, baseName + ".json");
                string cleanJson = JsonConvert.SerializeObject(session, Formatting.Indented);
                File.WriteAllText(cleanJsonPath, cleanJson);
            }

            if (config.EnableDebugJsonExport.Value)
            {
                Directory.CreateDirectory(memoryContext.RawDirectory);
                result.JsonPath = Path.Combine(memoryContext.RawDirectory, baseName + "_session.debug.json");
                string debugJson = JsonConvert.SerializeObject(session, Formatting.Indented);
                File.WriteAllText(result.JsonPath, debugJson);
            }

            if (config.SaveDiscord.Value)
            {
                string discordDir = Path.Combine(memoryContext.WorldDirectory, "Discord");
                Directory.CreateDirectory(discordDir);
                string discordPath = Path.Combine(discordDir, baseName + ".md");

                string discordMarkdown = $"# Valheim Výprava ({session.StartTimeUtc.ToLocalTime():yyyy-MM-dd})\n\n";
                discordMarkdown += $"**Trvání:** {session.Duration.TotalMinutes:F0} min\n";
                discordMarkdown += $"**Charakter:** {_fileNameBuilder.BuildBaseName(session).Split('_')[1]}\n\n";

                // Extract just the story paragraphs from the txt report if possible, else use first 3 paragraphs
                string[] lines = result.TxtReport.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                int storyStart = Array.FindIndex(lines, l => l.Contains("PŘÍBĚH") || l.Contains("STORY"));

                if (storyStart >= 0 && storyStart + 1 < lines.Length)
                {
                    discordMarkdown += lines[storyStart + 1] + "\n\n";
                }

                File.WriteAllText(discordPath, discordMarkdown);
            }

            if (config.EnableChronicleHistory.Value)
            {
                string historyPath = Path.Combine(memoryContext.WorldDirectory, "ChronicleHistory.md");
                string indexJsonPath = Path.Combine(memoryContext.WorldDirectory, "ChronicleIndex.json");

                string summaryLine = $"- **{session.StartTimeUtc.ToLocalTime():yyyy-MM-dd}**: Session duration {session.Duration.TotalMinutes:F0} min (ID: {session.SessionId})\n";
                File.AppendAllText(historyPath, summaryLine);
                // Also could write to ChronicleIndex.json here in a robust implementation
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

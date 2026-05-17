using System;
using System.IO;
using BepInEx;
using Newtonsoft.Json;
using ValheimSessionChronicle.Configuration;
using ValheimSessionChronicle.Models;
using ValheimSessionChronicle.Reporting;

namespace ValheimSessionChronicle.Storage
{
    public sealed class SessionStorage
    {
        private readonly ReportGenerator _reportGenerator;

        public SessionStorage(ReportGenerator reportGenerator)
        {
            _reportGenerator = reportGenerator;
        }

        public StorageResult Save(SessionData session, ChronicleConfig config)
        {
            string reportsDirectory = GetReportsDirectory();
            Directory.CreateDirectory(reportsDirectory);

            string safeServer = MakeSafeFileName(session.ServerName);
            string timestamp = session.StartTimeUtc.ToLocalTime().ToString("yyyy-MM-dd_HH-mm-ss");
            string baseName = $"{timestamp}_{safeServer}_{session.SessionId.Substring(0, 8)}";

            StorageResult result = new StorageResult();

            if (config.SaveTXT.Value)
            {
                result.TxtReport = _reportGenerator.Generate(session, config.IncludeCompactTimeline.Value);
                result.TxtPath = Path.Combine(reportsDirectory, baseName + ".txt");
                File.WriteAllText(result.TxtPath, result.TxtReport);
            }

            if (config.EnableDebugJsonExport.Value)
            {
                result.JsonPath = Path.Combine(reportsDirectory, baseName + ".debug.json");
                string json = JsonConvert.SerializeObject(session, Formatting.Indented);
                File.WriteAllText(result.JsonPath, json);
            }

            return result;
        }

        public static string GetReportsDirectory()
        {
            return Path.Combine(Paths.PluginPath, "ValheimSessionChronicle", "Reports");
        }

        private static string MakeSafeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "UnknownServer";
            }

            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }

            return value.Replace(' ', '_');
        }
    }
}

using System;
using System.IO;
using BepInEx;
using Newtonsoft.Json;
using ValheimSessionChronicle.Core;
using ValheimSessionChronicle.Models;

namespace ValheimSessionChronicle.WorldMemory
{
    public sealed class WorldMemoryStorage
    {
        public string GetWorldDirectory(SessionData session)
        {
            return Path.Combine(Paths.PluginPath, "ValheimSessionChronicle", "Reports", GetWorldFolderName(session));
        }

        public string GetRawDirectory(SessionData session)
        {
            return Path.Combine(GetWorldDirectory(session), "Raw");
        }

        public string GetMemoryPath(SessionData session)
        {
            string worldFolder = GetWorldFolderName(session);
            return Path.Combine(GetWorldDirectory(session), worldFolder + "WorldMemory.json");
        }

        public WorldMemoryData LoadOrCreate(SessionData session)
        {
            string path = GetMemoryPath(session);
            try
            {
                Directory.CreateDirectory(GetWorldDirectory(session));
                if (File.Exists(path))
                {
                    WorldMemoryData data = JsonConvert.DeserializeObject<WorldMemoryData>(File.ReadAllText(path));
                    if (data != null)
                    {
                        EnsureIdentity(data, session);
                        return data;
                    }
                }
            }
            catch (Exception ex)
            {
                ChronicleLogger.Warning($"World memory could not be loaded. A fresh memory will be used for this save: {ex.Message}");
            }

            WorldMemoryData fresh = new WorldMemoryData();
            EnsureIdentity(fresh, session);
            return fresh;
        }

        public void Save(SessionData session, WorldMemoryData memory)
        {
            try
            {
                string path = GetMemoryPath(session);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonConvert.SerializeObject(memory, Formatting.Indented));
            }
            catch (Exception ex)
            {
                ChronicleLogger.Warning($"World memory save failed: {ex.Message}");
            }
        }

        public string GetWorldFolderName(SessionData session)
        {
            string name = !string.IsNullOrWhiteSpace(session.WorldName)
                ? session.WorldName
                : session.ServerName;

            if (string.IsNullOrWhiteSpace(name))
            {
                name = "UnknownWorld";
            }

            return SanitizeFileName(name);
        }

        public string SanitizeFileName(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                value = value.Replace(invalid, '_');
            }

            value = value.Replace(' ', '_').Trim('_');
            return string.IsNullOrWhiteSpace(value) ? "UnknownWorld" : value;
        }

        private void EnsureIdentity(WorldMemoryData data, SessionData session)
        {
            data.WorldKey = GetWorldFolderName(session);
            data.WorldName = string.IsNullOrWhiteSpace(session.WorldName) ? data.WorldKey : session.WorldName;
            data.ServerName = session.ServerName ?? string.Empty;
        }
    }
}

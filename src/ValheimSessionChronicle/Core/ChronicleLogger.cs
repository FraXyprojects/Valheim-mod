using System;
using BepInEx.Logging;
using ValheimSessionChronicle.Configuration;

namespace ValheimSessionChronicle.Core
{
    internal static class ChronicleLogger
    {
        private static ManualLogSource _log;
        private static ChronicleConfig _config;

        public static void Initialize(ManualLogSource log, ChronicleConfig config)
        {
            _log = log;
            _config = config;
        }

        public static void Info(string message)
        {
            _log?.LogInfo(message);
        }

        public static void Verbose(string message)
        {
            if (_config?.EnableVerboseLogging?.Value == true)
            {
                _log?.LogInfo("[Verbose] " + message);
            }
        }

        public static void Warning(string message)
        {
            _log?.LogWarning(message);
        }

        public static void Error(Exception exception, string message)
        {
            _log?.LogError($"{message} {exception}");
        }
    }
}

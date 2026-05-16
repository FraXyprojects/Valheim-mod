using System;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using ValheimSessionChronicle.Configuration;
using ValheimSessionChronicle.Core;
using ValheimSessionChronicle.Discord;
using ValheimSessionChronicle.Patches;
using ValheimSessionChronicle.Reporting;
using ValheimSessionChronicle.Storage;

namespace ValheimSessionChronicle
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("valheim.exe")]
    [BepInProcess("valheim")]
    [BepInProcess("valheim.x86_64")]
    public sealed class ValheimSessionChroniclePlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "fraxy.valheim.sessionchronicle";
        public const string PluginName = "ValheimSessionChronicle";
        public const string PluginVersion = "1.0.0";

        internal static ValheimSessionChroniclePlugin Instance { get; private set; }

        internal ChronicleConfig ModConfig { get; private set; }
        internal SessionManager SessionManager { get; private set; }

        private Harmony _harmony;
        private SessionWatcher _watcher;

        private void Awake()
        {
            Instance = this;

            ModConfig = new ChronicleConfig(Config);
            ChronicleLogger.Initialize(Logger, ModConfig);

            SessionManager = new SessionManager(
                ModConfig,
                new SessionStorage(new ReportGenerator()),
                new DiscordWebhookClient());

            _watcher = gameObject.AddComponent<SessionWatcher>();
            _watcher.Initialize(SessionManager, ModConfig);
            DontDestroyOnLoad(gameObject);

            _harmony = new Harmony(PluginGuid);
            PatchRegistrar.ApplyAll(_harmony);

            ChronicleLogger.Info($"{PluginName} {PluginVersion} loaded. Client-side observer mode is active.");
        }

        private void OnApplicationQuit()
        {
            TryEndSession("Hra se ukončuje.", DisconnectReason.ApplicationQuit);
        }

        private void OnDestroy()
        {
            TryEndSession("Plugin byl odpojen.", DisconnectReason.PluginUnload);

            try
            {
                _harmony?.UnpatchSelf();
            }
            catch (Exception ex)
            {
                ChronicleLogger.Warning($"Harmony unpatch failed: {ex.Message}");
            }
        }

        private void TryEndSession(string reason, DisconnectReason disconnectReason)
        {
            try
            {
                SessionManager?.EndSession(reason, disconnectReason);
            }
            catch (Exception ex)
            {
                ChronicleLogger.Error(ex, "Failed to end the active Valheim session.");
            }
        }
    }
}

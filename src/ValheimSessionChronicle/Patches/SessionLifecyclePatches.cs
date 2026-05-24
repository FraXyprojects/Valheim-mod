using System;
using ValheimSessionChronicle.Core;

namespace ValheimSessionChronicle.Patches
{
    internal static class SessionLifecyclePatches
    {
        public static void ZNetShutdownPrefix()
        {
            EndSafely("Valheim network session is shutting down.", DisconnectReason.ZNetShutdown);
        }

        public static void GameLogoutPrefix()
        {
            EndSafely("Player used Valheim logout.", DisconnectReason.GameLogout);
        }

        private static void EndSafely(string reason, DisconnectReason disconnectReason)
        {
            try
            {
                ValheimSessionChroniclePlugin.Instance?.LifecycleManager?.NotifyDisconnectSignal(reason, disconnectReason);
            }
            catch (Exception ex)
            {
                ChronicleLogger.Error(ex, "Session lifecycle patch failed.");
            }
        }
    }
}

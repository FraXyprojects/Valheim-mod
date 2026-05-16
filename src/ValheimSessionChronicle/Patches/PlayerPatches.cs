using System;
using ValheimSessionChronicle.Core;
using ValheimSessionChronicle.Utility;

namespace ValheimSessionChronicle.Patches
{
    internal static class PlayerPatches
    {
        public static void PlayerDeathPostfix(Player __instance)
        {
            try
            {
                ValheimSessionChroniclePlugin.Instance?.SessionManager?.RecordPlayerDeath(__instance);
            }
            catch (Exception ex)
            {
                ChronicleLogger.Error(ex, "Player.OnDeath tracking failed.");
            }
        }

        public static void PlayerSpawnedPostfix(Player __instance)
        {
            try
            {
                ValheimSessionChroniclePlugin.Instance?.SessionManager?.RecordPlayerRespawn(__instance);
            }
            catch (Exception ex)
            {
                ChronicleLogger.Error(ex, "Player.OnSpawned tracking failed.");
            }
        }

        public static void PlayerSetSleepingPostfix(Player __instance, object[] __args)
        {
            try
            {
                bool sleeping = __args != null && __args.Length > 0 && __args[0] is bool value && value;
                if (sleeping)
                {
                    ValheimSessionChroniclePlugin.Instance?.SessionManager?.RecordSleeping(__instance);
                }
            }
            catch (Exception ex)
            {
                ChronicleLogger.Error(ex, "Player.SetSleeping tracking failed.");
            }
        }

        public static void CreateTombStonePostfix(Player __instance)
        {
            try
            {
                ValheimSessionChroniclePlugin.Instance?.SessionManager?.RecordTombstoneCreated(__instance);
            }
            catch (Exception ex)
            {
                ChronicleLogger.Error(ex, "Player.CreateTombStone tracking failed.");
            }
        }
    }
}

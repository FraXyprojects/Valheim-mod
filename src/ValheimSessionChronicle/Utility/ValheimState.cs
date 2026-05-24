using System;
using ValheimSessionChronicle.Core;

namespace ValheimSessionChronicle.Utility
{
    internal static class ValheimState
    {
        public static bool IsInPlayableSession()
        {
            try
            {
                return HasNetwork() && HasLocalPlayer();
            }
            catch
            {
                return false;
            }
        }

        public static bool HasNetwork()
        {
            try
            {
                return ZNet.instance != null;
            }
            catch
            {
                return false;
            }
        }

        public static bool HasLocalPlayer()
        {
            try
            {
                return Player.m_localPlayer != null;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsMultiplayerClient()
        {
            try
            {
                if (ZNet.instance == null)
                {
                    return false;
                }

                object isServer = ValheimReflection.Invoke(ZNet.instance, "IsServer");
                if (isServer is bool server)
                {
                    return !server;
                }
            }
            catch (Exception ex)
            {
                ChronicleLogger.Verbose($"Could not determine client/server mode: {ex.Message}");
            }

            return false;
        }
    }
}

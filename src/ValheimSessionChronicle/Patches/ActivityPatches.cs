using System;
using ValheimSessionChronicle.Core;
using ValheimSessionChronicle.Utility;

namespace ValheimSessionChronicle.Patches
{
    internal static class ActivityPatches
    {
        public static void PlacePiecePostfix(Player __instance, object[] __args)
        {
            try
            {
                ValheimSessionChroniclePlugin.Instance?.SessionManager?.RecordPiecePlacement(__instance, __args);
            }
            catch (Exception ex)
            {
                ChronicleLogger.Error(ex, "Player.PlacePiece tracking failed.");
            }
        }

        public static void CraftItemPostfix(Player __instance, object[] __args)
        {
            try
            {
                ValheimSessionChroniclePlugin.Instance?.SessionManager?.RecordCrafting(__instance, __args);
            }
            catch (Exception ex)
            {
                ChronicleLogger.Error(ex, "Player.CraftItem tracking failed.");
            }
        }

        public static void PickupPostfix(Humanoid __instance, object[] __args)
        {
            try
            {
                ValheimSessionChroniclePlugin.Instance?.SessionManager?.RecordItemPickup(__instance, __args);
            }
            catch (Exception ex)
            {
                ChronicleLogger.Error(ex, "Humanoid.Pickup tracking failed.");
            }
        }

        public static void PlayerPickupPostfix(Player __instance, object[] __args)
        {
            try
            {
                ValheimSessionChroniclePlugin.Instance?.SessionManager?.RecordItemPickup(__instance, __args);
            }
            catch (Exception ex)
            {
                ChronicleLogger.Error(ex, "Player.Pickup tracking failed.");
            }
        }

        public static void ItemDropPickupPostfix(ItemDrop __instance, object[] __args)
        {
            try
            {
                ValheimSessionChroniclePlugin.Instance?.SessionManager?.RecordItemDropPickup(__instance, __args);
            }
            catch (Exception ex)
            {
                ChronicleLogger.Error(ex, "ItemDrop.Pickup tracking failed.");
            }
        }

        public static void PortalTeleportPostfix(TeleportWorld __instance, object[] __args)
        {
            try
            {
                ValheimSessionChroniclePlugin.Instance?.SessionManager?.RecordPortalUse(__instance, __args);
            }
            catch (Exception ex)
            {
                ChronicleLogger.Error(ex, "TeleportWorld.Teleport tracking failed.");
            }
        }

        public static void BedInteractPostfix(Bed __instance, object[] __args)
        {
            try
            {
                Player player = ValheimNames.FindPlayerArgument(__args) ?? Player.m_localPlayer;
                ValheimSessionChroniclePlugin.Instance?.SessionManager?.RecordSleeping(player);
            }
            catch (Exception ex)
            {
                ChronicleLogger.Error(ex, "Bed.Interact tracking failed.");
            }
        }

        public static void ShipInteractPostfix(ShipControlls __instance, object[] __args)
        {
            try
            {
                ValheimSessionChroniclePlugin.Instance?.SessionManager?.RecordShipUse(__args);
            }
            catch (Exception ex)
            {
                ChronicleLogger.Error(ex, "ShipControlls.Interact tracking failed.");
            }
        }
    }
}

using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using ValheimSessionChronicle.Core;

namespace ValheimSessionChronicle.Patches
{
    internal static class PatchRegistrar
    {
        public static void ApplyAll(Harmony harmony)
        {
            // Session lifecycle hooks are backed up by SessionWatcher, so missing methods are not fatal.
            PatchAllOverloads(harmony, typeof(ZNet), "Shutdown", typeof(SessionLifecyclePatches), prefixName: nameof(SessionLifecyclePatches.ZNetShutdownPrefix));
            PatchAllOverloads(harmony, typeof(Game), "Logout", typeof(SessionLifecyclePatches), prefixName: nameof(SessionLifecyclePatches.GameLogoutPrefix));

            PatchAllOverloads(harmony, typeof(Player), "OnDeath", typeof(PlayerPatches), postfixName: nameof(PlayerPatches.PlayerDeathPostfix));
            PatchAllOverloads(harmony, typeof(Player), "OnSpawned", typeof(PlayerPatches), postfixName: nameof(PlayerPatches.PlayerSpawnedPostfix));
            PatchAllOverloads(harmony, typeof(Player), "SetSleeping", typeof(PlayerPatches), postfixName: nameof(PlayerPatches.PlayerSetSleepingPostfix));
            PatchAllOverloads(harmony, typeof(Player), "CreateTombStone", typeof(PlayerPatches), postfixName: nameof(PlayerPatches.CreateTombStonePostfix));

            PatchAllOverloads(harmony, typeof(Character), "OnDeath", typeof(CharacterPatches), postfixName: nameof(CharacterPatches.CharacterDeathPostfix));
            PatchAllOverloads(harmony, typeof(Character), "Damage", typeof(CharacterPatches), postfixName: nameof(CharacterPatches.CharacterDamagePostfix));

            PatchAllOverloads(harmony, typeof(Player), "PlacePiece", typeof(ActivityPatches), postfixName: nameof(ActivityPatches.PlacePiecePostfix));
            PatchAllOverloads(harmony, typeof(Player), "CraftItem", typeof(ActivityPatches), postfixName: nameof(ActivityPatches.CraftItemPostfix));
            PatchAllOverloads(harmony, typeof(Player), "Pickup", typeof(ActivityPatches), postfixName: nameof(ActivityPatches.PlayerPickupPostfix));
            PatchAllOverloads(harmony, typeof(Humanoid), "Pickup", typeof(ActivityPatches), postfixName: nameof(ActivityPatches.PickupPostfix));
            PatchAllOverloads(harmony, typeof(ItemDrop), "Pickup", typeof(ActivityPatches), postfixName: nameof(ActivityPatches.ItemDropPickupPostfix));
            PatchAllOverloads(harmony, typeof(TeleportWorld), "Teleport", typeof(ActivityPatches), postfixName: nameof(ActivityPatches.PortalTeleportPostfix));
            PatchAllOverloads(harmony, typeof(Bed), "Interact", typeof(ActivityPatches), postfixName: nameof(ActivityPatches.BedInteractPostfix));
            PatchAllOverloads(harmony, typeof(ShipControlls), "Interact", typeof(ActivityPatches), postfixName: nameof(ActivityPatches.ShipInteractPostfix));
        }

        private static void PatchAllOverloads(
            Harmony harmony,
            Type targetType,
            string methodName,
            Type patchType,
            string prefixName = null,
            string postfixName = null)
        {
            try
            {
                MethodInfo prefix = string.IsNullOrWhiteSpace(prefixName) ? null : AccessTools.Method(patchType, prefixName);
                MethodInfo postfix = string.IsNullOrWhiteSpace(postfixName) ? null : AccessTools.Method(patchType, postfixName);

                MethodInfo[] originals = AccessTools.GetDeclaredMethods(targetType)
                    .Where(method => method.Name == methodName && !method.IsAbstract)
                    .ToArray();

                if (originals.Length == 0)
                {
                    ChronicleLogger.Warning($"Harmony target not found: {targetType.FullName}.{methodName}. Tracking will use fallback logic if available.");
                    return;
                }

                foreach (MethodInfo original in originals)
                {
                    try
                    {
                        harmony.Patch(
                            original,
                            prefix == null ? null : new HarmonyMethod(prefix),
                            postfix == null ? null : new HarmonyMethod(postfix));

                        ChronicleLogger.Verbose($"Patched {targetType.Name}.{original.Name}({original.GetParameters().Length} args).");
                    }
                    catch (Exception ex)
                    {
                        ChronicleLogger.Warning($"Failed to patch {targetType.Name}.{methodName}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                ChronicleLogger.Warning($"Patch registration failed for {targetType.FullName}.{methodName}: {ex.Message}");
            }
        }
    }
}

using System;
using ValheimSessionChronicle.Core;

namespace ValheimSessionChronicle.Patches
{
    internal static class CharacterPatches
    {
        public static void CharacterDeathPostfix(Character __instance)
        {
            try
            {
                ValheimSessionChroniclePlugin.Instance?.SessionManager?.RecordCharacterDeath(__instance);
            }
            catch (Exception ex)
            {
                ChronicleLogger.Error(ex, "Character.OnDeath tracking failed.");
            }
        }

        public static void CharacterDamagePostfix(Character __instance, object[] __args)
        {
            try
            {
                object hitData = null;
                if (__args != null)
                {
                    for (int index = 0; index < __args.Length; index++)
                    {
                        if (__args[index] != null && __args[index].GetType().Name == "HitData")
                        {
                            hitData = __args[index];
                            break;
                        }
                    }
                }

                if (hitData != null)
                {
                    ValheimSessionChroniclePlugin.Instance?.SessionManager?.RecordCombatDamage(__instance, hitData);
                }
            }
            catch (Exception ex)
            {
                ChronicleLogger.Error(ex, "Character.Damage tracking failed.");
            }
        }
    }
}

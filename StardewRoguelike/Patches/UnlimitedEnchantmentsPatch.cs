using HarmonyLib;
using StardewValley.Tools;

namespace StardewRoguelike.Patches
{
    [HarmonyPatch(typeof(MeleeWeapon), nameof(MeleeWeapon.GetMaxForges))]
    internal class UnlimitedEnchantmentsPatch
    {
        public static void Postfix(ref int __result)
        {
            __result = 999;
        }
    }
}

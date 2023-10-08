using HarmonyLib;
using StardewValley.Monsters;

namespace StardewRoguelike.Patches
{
    [HarmonyPatch(typeof(Bug), nameof(Bug.takeDamage))]
    internal class NoArmoredBugPatch
    {
        public static bool Prefix(Bug __instance)
        {
            __instance.isArmoredBug.Value = false;
            return true;
        }
    }
}

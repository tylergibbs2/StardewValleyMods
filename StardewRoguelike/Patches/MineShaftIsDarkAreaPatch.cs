using HarmonyLib;
using StardewValley.Locations;

namespace StardewRoguelike.Patches
{
    [HarmonyPatch(typeof(MineShaft), "isDarkArea")]
    internal class MineShaftIsDarkAreaPatch
    {
        public static bool Prefix(MineShaft __instance, ref bool __result)
        {
            bool loadedDarkArea = __instance.loadedDarkArea;

            if (loadedDarkArea)
                __result = true;

            return false;
        }
    }
}

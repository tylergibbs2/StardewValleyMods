using HarmonyLib;
using StardewValley.Locations;

namespace StardewRoguelike.Patches
{
    [HarmonyPatch(typeof(MineShaft), "clearInactiveMines")]
    internal class ClearInactiveMinesPatch
    {
        public static bool Prefix()
        {
            return false;
        }
    }
}

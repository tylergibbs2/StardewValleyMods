using HarmonyLib;
using StardewValley.Locations;

namespace StardewRoguelike.Patches
{
    [HarmonyPatch(typeof(MineShaft), nameof(MineShaft.spawnFlyingMonsterOffScreen))]
    internal class MineShaftDisableRandomBug
    {
        public static bool Prefix()
        {
            return false;
        }
    }
}

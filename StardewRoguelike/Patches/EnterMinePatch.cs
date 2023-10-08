using HarmonyLib;
using StardewValley;

namespace StardewRoguelike.Patches
{
    [HarmonyPatch(typeof(Game1), nameof(Game1.enterMine))]
    internal class EnterMinePatch
    {
        public static bool Prefix()
        {
            Roguelike.NextFloor();

            Game1.inMine = true;
            // Warp to mine level 1. We handle choosing the map
            // elsewhere.
            Game1.warpFarmer($"UndergroundMine1/{Roguelike.CurrentLevel}", 6, 6, 2);

            return false;
        }
    }
}

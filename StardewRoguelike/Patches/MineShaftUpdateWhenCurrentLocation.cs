using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley.Locations;

namespace StardewRoguelike.Patches
{
    [HarmonyPatch(typeof(MineShaft), nameof(MineShaft.UpdateWhenCurrentLocation))]
    internal class MineShaftUpdateWhenCurrentLocation
    {
        public static void Postfix(MineShaft __instance, GameTime time)
        {
            ChallengeFloor.DoUpdate(__instance, time);
        }
    }
}

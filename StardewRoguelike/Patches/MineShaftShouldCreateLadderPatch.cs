using HarmonyLib;
using StardewRoguelike.ChallengeFloors;
using StardewRoguelike.VirtualProperties;
using StardewValley.Locations;

namespace StardewRoguelike.Patches
{
    [HarmonyPatch(typeof(MineShaft), nameof(MineShaft.shouldCreateLadderOnThisLevel))]
    internal class MineShaftShouldCreateLadderPatch
    {
        public static bool Prefix(MineShaft __instance, ref bool __result)
        {
            if (ChallengeFloor.IsChallengeFloor(__instance))
            {
                ChallengeBase challenge = __instance.get_MineShaftChallengeFloor();
                __result = challenge.ShouldSpawnLadder(__instance);
                return false;
            }

            return true;
        }
    }
}

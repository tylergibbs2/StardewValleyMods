using HarmonyLib;
using StardewRoguelike.Run;
using StardewValley.Locations;

namespace StardewRoguelike.Patches
{
    /// <summary>
    /// Makes a shrine floor one that has to be cleared before it lets anyone past.
    ///
    /// A shrine floor used to be a mining floor with a statue added, which made the
    /// mining floor beside it in the menu pointless: the shrine gave everything the
    /// mineshaft gave and a perk on top. The two now ask for different things. A
    /// mineshaft is quick and can be left whenever a way down turns up; a shrine is
    /// infested, and the way down arrives with the last monster.
    ///
    /// The base game already has this - it is what an infested floor is - so the
    /// ladder rules, the "monsters remain" wording and the guarantee that something
    /// is alive to kill all come with it.
    /// </summary>
    [HarmonyPatch(typeof(MineShaft), nameof(MineShaft.mustKillAllMonstersToAdvance))]
    internal class MustKillAllMonstersPatch
    {
        public static void Postfix(MineShaft __instance, ref bool __result)
        {
            if (__result)
                return;

            __result = RunRoute.KindFor(Roguelike.GetLevelFromMineshaft(__instance)) == FloorKind.Shrine;
        }
    }
}

using HarmonyLib;
using StardewValley.Locations;
using StardewValley.Monsters;
using System;

namespace StardewRoguelike.Patches
{
    /// <summary>
    /// Has the last word on which monster the mine hands back.
    /// </summary>
    /// <remarks>
    /// One patch and not two. The filtering below could as easily have been its own
    /// postfix on the same method, but Harmony does not promise an order between
    /// two of them - and the order matters here, since the bat upgrade has to apply
    /// to whatever the filter settles on rather than be thrown away by it.
    /// </remarks>
    [HarmonyPatch(typeof(MineShaft), nameof(MineShaft.getMonsterForThisLevel))]
    internal class GetMonsterForLevelPatch
    {
        /// <summary>How many times to ask again before taking what is offered.</summary>
        private const int Rerolls = 16;

        /// <summary>Whether a reroll is already running on this thread.</summary>
        /// <remarks>
        /// The reroll calls the very method this is a postfix on, so without this
        /// each unwanted monster starts another sixteen rerolls inside the one
        /// already running. Going deep is unlikely and costs sixteen to the power
        /// of it, which is the wrong shape of risk for something that runs while a
        /// floor is being built.
        /// </remarks>
        [ThreadStatic]
        private static bool rerolling;

        public static void Postfix(ref Monster __result, MineShaft __instance, int level, int xTile, int yTile)
        {
            Decline(ref __result, __instance, level, xTile, yTile);

            int mineLevel = Roguelike.GetLevelFromMineshaft(__instance);
            bool isHardSkullCaverns = Constants.ScalingOrder[^2] <= mineLevel % 48 && mineLevel % 48 <= Constants.ScalingOrder[^1];
            bool isLooped = mineLevel > Constants.ScalingOrder[^1];

            if (__result is Bat bat && isHardSkullCaverns && isLooped)
                __result = new Bat(bat.Position, 1000);
        }

        /// <summary>
        /// Asks again where the mine offers something no floor wants.
        /// </summary>
        /// <remarks>
        /// Done here rather than at each spawner because normal floors are populated
        /// by the game's own code, so this is the one place every path goes through
        /// - and a floor written later inherits it without knowing it exists.
        /// </remarks>
        private static void Decline(ref Monster result, MineShaft mine, int level, int xTile, int yTile)
        {
            if (rerolling || !MonsterRoster.IsUnwanted(result))
                return;

            rerolling = true;
            try
            {
                for (int tries = 0; tries < Rerolls; tries++)
                {
                    // The same draw the caller made, so the depth keeps its own mix
                    // of monsters and this only declines one of them.
                    Monster again = mine.getMonsterForThisLevel(level, xTile, yTile);

                    if (!MonsterRoster.IsUnwanted(again))
                    {
                        result = again;
                        return;
                    }
                }
            }
            finally
            {
                rerolling = false;
            }
        }
    }
}

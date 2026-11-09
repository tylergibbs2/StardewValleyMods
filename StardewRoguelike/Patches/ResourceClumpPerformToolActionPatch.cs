using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace StardewRoguelike.Patches
{
    /// <summary>
    /// Sends a broken boulder to the keystones, which is where the burst lives.
    /// </summary>
    /// <remarks>
    /// Boulders take tool hits rather than being destroyed outright, so the hook is
    /// the return value: performToolAction answers true only on the hit that finishes
    /// one off. Hooking the hits instead would burst on every swing at a boulder that
    /// is still standing.
    /// </remarks>
    [HarmonyPatch(typeof(ResourceClump), nameof(ResourceClump.performToolAction))]
    internal class ResourceClumpPerformToolActionPatch
    {
        public static void Postfix(ResourceClump __instance, Tool t, Vector2 tileLocation, bool __result)
        {
            if (!__result || t is null)
                return;

            if (t.getLastFarmerToUse() != Game1.player)
                return;

            if (__instance.Location is not GameLocation location)
                return;

            Keystones.OnBoulderBroken(location, (int)tileLocation.X, (int)tileLocation.Y, Game1.player);
        }
    }
}

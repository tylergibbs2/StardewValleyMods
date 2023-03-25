using System;
using HarmonyLib;
using StardewValley;
using StardewValley.BellsAndWhistles;

namespace Circuit.Patches
{
    [HarmonyPatch(typeof(Train), nameof(Train.Update))]
    internal class TrainUpdatePatch
    {
        public static void Postfix(Train __instance, GameLocation location)
        {
            if (!ModEntry.ShouldPatch())
                return;

            if (!Game1.eventUp && location.Equals(Game1.currentLocation))
            {
                if (Game1.player.GetBoundingBox().Intersects(__instance.getBoundingBox()))
                    ModEntry.Instance.TaskManager?.OnHitByTrain();
            }
        }
    }
}

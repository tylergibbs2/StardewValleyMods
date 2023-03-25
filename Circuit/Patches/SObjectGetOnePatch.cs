using System.Linq;
using Circuit.Events;
using HarmonyLib;

namespace Circuit.Patches
{
    [HarmonyPatch(typeof(SObject), nameof(SObject.getOne))]
    internal class SObjectGetOnePatch
    {
        public static bool Prefix(SObject __instance)
        {
            if (!ModEntry.ShouldPatch(EventType.BountifulHarvest))
                return true;

            if (BountifulHarvest.HarvestIndices.Contains(__instance.ParentSheetIndex))
            {
                __instance.Quality = __instance.Quality switch
                {
                    0 => 1,
                    1 => 2,
                    2 => 4,
                    4 => 4,
                    _ => 0
                };
            }

            return true;
        }
    }
}

using HarmonyLib;
using StardewValley.Objects;

namespace Circuit.Patches
{
    [HarmonyPatch(typeof(FishTankFurniture), nameof(FishTankFurniture.checkForAction))]
    internal class FishTankCheckForAction
    {
        public static void Postfix(FishTankFurniture __instance, bool __result)
        {
            if (!ModEntry.ShouldPatch())
                return;

            if (__result)
                ModEntry.Instance.TaskManager?.OnFishTankCheckForAction(__instance);
        }
    }
}

using HarmonyLib;
using StardewValley.Buildings;

namespace Circuit.Patches
{
    [HarmonyPatch(typeof(Building), nameof(Building.dayUpdate))]
    internal class BuildingDayUpdatePatch
    {
        public static bool Prefix(Building __instance, out int __state)
        {
            __state = 0;
            if (!ModEntry.ShouldPatch())
                return true;

            int buildingUpsertDays = __instance.daysOfConstructionLeft.Value + __instance.daysUntilUpgrade.Value;
            __state = buildingUpsertDays;

            return true;
        }

        public static void Postfix(Building __instance, int __state)
        {
            if (!ModEntry.ShouldPatch() || __state == 0)
                return;

            int buildingUpsertDays = __instance.daysOfConstructionLeft.Value + __instance.daysUntilUpgrade.Value;
            if (buildingUpsertDays == 0 & __state != 0)
                ModEntry.Instance.TaskManager?.OnBuildingComplete(__instance.buildingType.Value);
        }
    }
}

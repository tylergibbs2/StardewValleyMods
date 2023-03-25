using HarmonyLib;
using StardewValley;

namespace Circuit.Patches
{
    [HarmonyPatch(typeof(Farm), nameof(Farm.addGrandpaCandles))]
    internal class FarmGrandpaCandlesPatch
    {
        public static void Postfix(Farm __instance)
        {
            if (!ModEntry.ShouldPatch())
                return;

            ModEntry.Instance.TaskManager?.OnGrandpaCandles(__instance);
        }
    }
}

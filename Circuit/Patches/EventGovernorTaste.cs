using HarmonyLib;
using StardewValley;

namespace Circuit.Patches
{
    [HarmonyPatch(typeof(Event), "governorTaste")]
    internal class EventGovernorTaste
    {
        public static void Postfix(Event __instance)
        {
            if (!ModEntry.ShouldPatch())
                return;

            ModEntry.Instance.TaskManager?.OnEventGovernorTaste(__instance);
        }
    }
}

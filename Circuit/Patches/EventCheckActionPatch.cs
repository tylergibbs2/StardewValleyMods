using HarmonyLib;
using StardewValley;
using xTile.Dimensions;

namespace Circuit.Patches
{
    [HarmonyPatch(typeof(Event), nameof(Event.checkAction))]
    internal class EventCheckActionPatch
    {
        public static bool Prefix(Event __instance, Location tileLocation) {
            if (!ModEntry.ShouldPatch())
                return true;

            ModEntry.Instance.TaskManager?.OnEventCheckAction(__instance, tileLocation);
            return true;
        }
    }
}

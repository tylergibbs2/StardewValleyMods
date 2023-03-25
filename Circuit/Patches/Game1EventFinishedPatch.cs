using HarmonyLib;
using StardewValley;

namespace Circuit.Patches
{
    [HarmonyPatch(typeof(Game1), nameof(Game1.eventFinished))]
    internal class Game1EventFinishedPatch
    {
        public static bool Prefix()
        {
            if (!ModEntry.ShouldPatch())
                return true;

            if (Game1.currentLocation.currentEvent is not null)
                ModEntry.Instance.TaskManager?.OnEventEnd(Game1.currentLocation.currentEvent);

            return true;
        }
    }
}

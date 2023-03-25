using HarmonyLib;
using StardewValley;
using StardewValley.Objects;

namespace Circuit.Patches
{
    [HarmonyPatch(typeof(TV), nameof(TV.checkForAction))]
    internal class TVCheckActionPatch
    {
        public static bool Prefix(ref bool __result, bool justCheckingForActivity)
        {
            if (!ModEntry.ShouldPatch() || justCheckingForActivity)
                return true;

            if (EventManager.EventIsActive(EventType.PoorService))
            {
                Game1.drawObjectDialogue("The screen is too fuzzy to make anything out.");
                __result = true;
                return false;
            }

            return true;
        }
    }
}

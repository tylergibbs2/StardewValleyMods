using HarmonyLib;
using StardewValley;

namespace Circuit.Patches
{
    [HarmonyPatch(typeof(Farmer), "eatObject")]
    internal class DisableEatingPatch
    {
        public static bool Prefix(SObject o)
        {
            if (!ModEntry.ShouldPatch() || Utility.IsNormalObjectAtParentSheetIndex(o, 434))
                return true;

            if (EventManager.EventIsActive(EventType.Nauseous))
            {
                Game1.drawObjectDialogue("You feel too sick to eat.");
                return false;
            }

            return true;
        }
    }
}

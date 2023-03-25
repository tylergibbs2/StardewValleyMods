using HarmonyLib;

namespace Circuit.Patches
{
    [HarmonyPatch(typeof(SObject), nameof(SObject.ApplySprinkler))]
    internal class ApplySprinklerPatch
    {
        public static bool Prefix()
        {
            if (!ModEntry.ShouldPatch())
                return true;

            if (EventManager.EventIsActive(EventType.WaterShortage))
                return false;

            return true;
        }
    }

    [HarmonyPatch(typeof(SObject), nameof(SObject.ApplySprinklerAnimation))]
    internal class ApplySprinklerAnimationPatch
    {
        public static bool Prefix()
        {
            if (!ModEntry.ShouldPatch())
                return true;

            if (EventManager.EventIsActive(EventType.WaterShortage))
                return false;

            return true;
        }
    }
}

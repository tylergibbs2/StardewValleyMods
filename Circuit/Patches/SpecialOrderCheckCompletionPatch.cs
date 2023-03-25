using HarmonyLib;
using StardewValley;

namespace Circuit.Patches
{
    [HarmonyPatch(typeof(SpecialOrder), nameof(SpecialOrder.CheckCompletion))]
    internal class SpecialOrderCheckCompletionPatch
    {
        public static void Postfix(SpecialOrder __instance)
        {
            if (!ModEntry.ShouldPatch())
                return;

            if (__instance.questState.Value == SpecialOrder.QuestState.Complete)
                ModEntry.Instance.TaskManager?.OnSpecialOrderCompleted();
        }
    }
}

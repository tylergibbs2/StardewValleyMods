using HarmonyLib;
using StardewValley.Quests;

namespace Circuit.Patches
{
    [HarmonyPatch(typeof(Quest), nameof(Quest.questComplete))]
    internal class QuestCompletePatch
    {
        public static void Postfix(Quest __instance)
        {
            if (!ModEntry.ShouldPatch())
                return;

            ModEntry.Instance.TaskManager?.OnJournalQuestComplete(__instance.id.Value);
        }
    }
}

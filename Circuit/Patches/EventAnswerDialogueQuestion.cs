using HarmonyLib;
using StardewValley;

namespace Circuit.Patches
{
    [HarmonyPatch(typeof(Event), nameof(Event.answerDialogueQuestion))]
    internal class EventAnswerDialogueQuestion
    {
        public static void Postfix()
        {
            if (!ModEntry.ShouldPatch() || Game1.player.dancePartner.Value is null)
                return;

            ModEntry.Instance.TaskManager?.OnFlowerDancePartnerAcquired();
        }
    }
}

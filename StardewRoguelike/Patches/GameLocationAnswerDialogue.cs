using HarmonyLib;
using StardewValley;
using StardewValley.Locations;

namespace StardewRoguelike.Patches
{
    [HarmonyPatch(typeof(GameLocation), "answerDialogueAction")]
    internal class GameLocationAnswerDialogue
    {
        public static void Postfix(GameLocation __instance, ref bool __result, string questionAndAnswer, string[] questionParams)
        {
            if (Roguelike.AnswerDialogueAction(__instance, questionAndAnswer, questionParams))
                __result = true;

            if (__instance is MineShaft mine)
            {
                if (ChallengeFloor.AnswerDialogueAction(mine, questionAndAnswer, questionParams))
                    __result = true;
                else if (Merchant.AnswerDialogueAction(mine, questionAndAnswer, questionParams))
                    __result = true;
            }
        }
    }
}

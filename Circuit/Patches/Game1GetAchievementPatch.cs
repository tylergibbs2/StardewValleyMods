using HarmonyLib;
using StardewValley;

namespace Circuit.Patches
{
    [HarmonyPatch(typeof(Game1), nameof(Game1.getAchievement))]
    internal class Game1GetAchievementPatch
    {
        public static void Postfix(int which)
        {
            if (!ModEntry.ShouldPatch())
                return;

            ModEntry.Instance.TaskManager?.OnAchievementEarned(which);
        }
    }
}

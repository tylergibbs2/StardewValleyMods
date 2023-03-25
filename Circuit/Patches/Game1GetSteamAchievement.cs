using HarmonyLib;
using StardewValley;

namespace Circuit.Patches
{
    [HarmonyPatch(typeof(Game1), nameof(Game1.getSteamAchievement))]
    internal class Game1GetSteamAchievement
    {
        public static void Postfix(string which)
        {
            if (!ModEntry.ShouldPatch())
                return;

            ModEntry.Instance.TaskManager?.OnSteamAchievementEarned(which);
        }
    }
}

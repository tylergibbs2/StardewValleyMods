using HarmonyLib;
using StardewValley;

namespace StardewRoguelike.Patches
{
    [HarmonyPatch(typeof(Game1), nameof(Game1.getAchievement))]
    class DisableAchievements
    {
        public static bool Prefix()
        {
            return false;
        }
    }
}

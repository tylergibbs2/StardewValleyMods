using HarmonyLib;
using StardewValley;

namespace Circuit.Patches
{
    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.blacksmith))]
    internal class GameLocationBlacksmithPatch
    {
        public static bool Prefix(out Tool? __state)
        {
            __state = null;
            if (!ModEntry.ShouldPatch())
                return true;

            __state = Game1.player.toolBeingUpgraded.Value;
            return true;
        }

        public static void Postfix(Tool? __state)
        {
            if (!ModEntry.ShouldPatch() || __state is null)
                return;

            if (Game1.player.toolBeingUpgraded.Value is null)
                ModEntry.Instance.TaskManager?.OnToolUpgrade(__state);
        }
    }
}

using HarmonyLib;
using StardewValley.Minigames;

namespace Circuit.Patches
{
    [HarmonyPatch(typeof(MineCart), nameof(MineCart.Die))]
    internal class MineCartDiePatch
    {
        public static void Postfix()
        {
            if (!ModEntry.ShouldPatch())
                return;

            ModEntry.Instance.TaskManager?.OnMineCartDied();
        }
    }
}

using HarmonyLib;
using StardewValley.TerrainFeatures;

namespace Circuit.Patches
{
    [HarmonyPatch(typeof(Tree), nameof(Tree.UpdateTapperProduct))]
    internal class TreeUpdateTapperPatch
    {
        public static void Postfix(Tree __instance)
        {
            if (!ModEntry.ShouldPatch())
                return;

            ModEntry.Instance.TaskManager?.OnTreeUpdateTapperProduct(__instance);
        }
    }
}

using HarmonyLib;
using StardewValley;

namespace Circuit.Patches
{
    [HarmonyPatch(typeof(Farmer), nameof(Farmer.shippedBasic))]
    internal class FarmerShippedBasicPatch
    {
        public static void Postfix(int index)
        {
            if (!ModEntry.ShouldPatch())
                return;

            ModEntry.Instance.TaskManager?.OnItemShipped(index);
        }
    }
}

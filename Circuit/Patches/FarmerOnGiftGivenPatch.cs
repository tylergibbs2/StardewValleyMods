using HarmonyLib;
using StardewValley;

namespace Circuit.Patches
{
    [HarmonyPatch(typeof(Farmer), nameof(Farmer.onGiftGiven))]
    internal class FarmerOnGiftGivenPatch
    {
        public static void Postfix(NPC npc, SObject item)
        {
            if (!ModEntry.ShouldPatch() || item.bigCraftable.Value)
                return;

            if (Game1.player.hasItemBeenGifted(npc, item.ParentSheetIndex))
                ModEntry.Instance.TaskManager?.OnItemGifted(npc, item);
        }
    }
}

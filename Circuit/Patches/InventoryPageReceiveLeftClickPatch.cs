using HarmonyLib;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;

namespace Circuit.Patches
{
    [HarmonyPatch(typeof(InventoryPage), nameof(InventoryPage.receiveLeftClick))]
    internal class InventoryPageReceiveLeftClickPatch
    {
        public static bool Prefix(out Hat? __state)
        {
            __state = Game1.player.hat.Value;
            return true;
        }

        public static void Postfix(Hat? __state)
        {
            if (!ModEntry.ShouldPatch())
                return;

            if (__state != Game1.player.hat.Value && Game1.player.hat.Value is not null)
                ModEntry.Instance.TaskManager?.OnHatEquipped(Game1.player.hat.Value);
        }
    }
}

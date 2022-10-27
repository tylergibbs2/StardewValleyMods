using StardewValley.Menus;

namespace BattleRoyale.Patches
{
    class DisableHatRemoval : Patch
    {
        protected override PatchDescriptor GetPatchDescriptor() => new(typeof(InventoryPage), "receiveLeftClick");

        public static bool Prefix(InventoryPage __instance, int x, int y)
        {
            foreach (ClickableComponent c in __instance.equipmentIcons)
            {
                if (!c.containsPoint(x, y))
                {
                    continue;
                }
                if (c.name == "Hat")
                    return false;
            }

            return true;
        }
    }
}

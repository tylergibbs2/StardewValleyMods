using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewRoguelike.UI;
using StardewValley;
using StardewValley.Enchantments;
using StardewValley.Inventories;
using StardewValley.Menus;
using System;
using System.Collections.Generic;

namespace StardewRoguelike.Patches
{
    internal class FreeForgePatch : Patch
    {
        protected override PatchDescriptor GetPatchDescriptor() => new(new List<string>() { "SpaceCore.Interface.NewForgeMenu", "StardewValley.Menus.ForgeMenu" }, "GetForgeCost");

        public static bool Prefix(ref int __result)
        {
            __result = 0;
            return false;
        }
    }

    /// <summary>
    /// Lets the forge work with no cinder shards at all.
    ///
    /// Zeroing the cost was never enough on its own. The menu asks
    /// Items.ContainsId("(O)848", cost) both to decide what to say and to decide
    /// whether to craft, and that method looks the item up before it looks at the
    /// count - so a cost of zero is still refused by a player carrying none. The
    /// forge floor hands out no shards and the run has no other source, so the floor
    /// only ever worked for someone who happened to have some already.
    ///
    /// Scoped as tightly as it can be: this floor's own menu, and that one item.
    /// </summary>
    internal class FreeForgeShardsPatch : Patch
    {
        private const string CinderShard = "(O)848";

        protected override PatchDescriptor GetPatchDescriptor() => new(
            typeof(Inventory),
            nameof(Inventory.ContainsId),
            new[] { typeof(string), typeof(int) });

        public static bool Prefix(string itemId, ref bool __result)
        {
            if (Game1.activeClickableMenu is not LimitedForgeMenu)
                return true;

            // Compared both ways rather than through ItemRegistry, since this runs
            // on every inventory lookup the menu makes.
            if (itemId != CinderShard && itemId != "848")
                return true;

            __result = true;
            return false;
        }
    }

    internal class FreeForgePatch2 : Patch
    {
        protected override PatchDescriptor GetPatchDescriptor() => new(new List<string>() { "SpaceCore.Interface.NewForgeMenu", "StardewValley.Menus.ForgeMenu" }, "GetForgeCostAtLevel");

        public static bool Prefix(ref int __result)
        {
            __result = 0;
            return false;
        }
    }

    internal class LimitedForgePatch : Patch
    {
        protected override PatchDescriptor GetPatchDescriptor() => new(new List<string>() { "SpaceCore.Interface.NewForgeMenu", "StardewValley.Menus.ForgeMenu" }, "IsValidCraft");

        public static bool Prefix(ForgeMenu __instance, ref bool __result)
        {
            if (__instance is LimitedForgeMenu limitedForgeMenu && limitedForgeMenu.UsesLeft == 0)
            {
                __result = false;
                return false;
            }

            return true;
        }
    }

    internal class LimitedForgePatch2 : Patch
    {
        protected override PatchDescriptor GetPatchDescriptor() => new(new List<string>() { "SpaceCore.Interface.NewForgeMenu", "StardewValley.Menus.ForgeMenu" }, "CraftItem");

        public static void Postfix(ForgeMenu __instance, bool forReal = false)
        {
            if (__instance is LimitedForgeMenu limitedForgeMenu && forReal)
                limitedForgeMenu.UsesLeft--;
        }
    }

    internal class LimitedForgePatch3 : Patch
    {
        protected override PatchDescriptor GetPatchDescriptor() => new(new List<string>() { "SpaceCore.Interface.NewForgeMenu", "StardewValley.Menus.ForgeMenu" }, "draw");

        public static void Postfix(ForgeMenu __instance, SpriteBatch b)
        {
            if (__instance is not LimitedForgeMenu limitedForgeMenu)
                return;

            Color usesColor = limitedForgeMenu.UsesLeft switch
            {
                3 => Color.Green,
                2 => Color.Yellow,
                1 => Color.Red,
                _ => Color.Black
            };

            Utility.drawBoldText(
                b,
                I18n.UI_LimitedForgeMenu_UsesLeft(),
                Game1.smallFont,
                new Vector2(limitedForgeMenu.xPositionOnScreen + 292, limitedForgeMenu.yPositionOnScreen + 310),
                Game1.textColor
            );

            Utility.drawBoldText(
                b,
                limitedForgeMenu.UsesLeft.ToString(),
                Game1.smallFont,
                new Vector2(limitedForgeMenu.xPositionOnScreen + 340, limitedForgeMenu.yPositionOnScreen + 338),
                usesColor
            );

            if (!__instance.hoverText.Equals(""))
            {
                IClickableMenu.drawHoverText(b, __instance.hoverText, Game1.smallFont, (__instance.heldItem != null) ? 32 : 0, (__instance.heldItem != null) ? 32 : 0);
            }
            else if (__instance.hoveredItem != null)
            {
                if (__instance.hoveredItem == __instance.craftResultDisplay.item && Utility.IsNormalObjectAtParentSheetIndex(__instance.rightIngredientSpot.item, "74"))
                    BaseEnchantment.hideEnchantmentName = true;

                IClickableMenu.drawToolTip(b, __instance.hoveredItem.getDescription(), __instance.hoveredItem.DisplayName, __instance.hoveredItem, __instance.heldItem != null);
                BaseEnchantment.hideEnchantmentName = false;
            }
            if (__instance.heldItem != null)
                __instance.heldItem.drawInMenu(b, new Vector2(Game1.getOldMouseX() + 8, Game1.getOldMouseY() + 8), 1f);

            __instance.drawMouse(b);
        }
    }

    [HarmonyPatch(typeof(Utility), "CollectOrDrop", new Type[] { typeof(Item), typeof(int) })]
    internal class UtilityCollectOrDrop
    {
        public static bool Prefix(ref bool __result, Item item, int direction)
        {
            if (item is not null && item.ParentSheetIndex == 848)
            {
                __result = false;
                return false;
            }

            return true;
        }
    }
}

using HarmonyLib;
using Microsoft.Xna.Framework;
using System.Reflection;
using StardewValley;
using StardewValley.Buffs;
using StardewValley.Menus;
using System.Collections.Generic;

namespace StardewRoguelike.Patches
{
    /// <summary>
    /// Places the buff icons in the space the HUD is drawn in.
    ///
    /// BuffsDisplay measures itself with GraphicsDevice.Viewport.GetTitleSafeArea(),
    /// which is the back buffer. Every other thing on the HUD measures itself against
    /// Game1.uiViewport - DayTimeMoneyBox puts itself at uiViewport.Width - 300, the
    /// same corner - and the money box lands where it should while the buff icons do
    /// not. On a 1513 wide UI they were being laid out from x=2205, almost a thousand
    /// pixels past the edge, so nothing has reached the buff bar: not the curses, and
    /// not the run's own adrenaline buff either.
    ///
    /// This replaces the method rather than correcting it afterwards. Correcting it
    /// left the base game free to lay the icons out from the back buffer first, and
    /// whichever of the two ran last won.
    /// </summary>
    [HarmonyPatch(typeof(BuffsDisplay), "updatePosition")]
    internal class BuffsDisplayPositionPatch
    {
        /// <summary>The room the base game leaves on the right for the money box.</summary>
        private const int RightMargin = 300;

        /// <summary>The size the base game gives the bar: four icons across, one down.</summary>
        private const int BarWidth = 288;
        private const int BarHeight = 64;

        /// <summary>The icons are 64px squares with an 8px gap, which is also the inset.</summary>
        private const int IconSize = 64;
        private const int IconGap = 8;

        private static readonly MethodBase? ResetIcons =
            AccessTools.Method(typeof(BuffsDisplay), "resetIcons");

        public static bool Prefix(BuffsDisplay __instance)
        {
            int x = Game1.uiViewport.Width - RightMargin - BarWidth;
            int y = IconGap;

            bool changed = x != __instance.xPositionOnScreen
                           || y != __instance.yPositionOnScreen
                           || BarWidth != __instance.width
                           || BarHeight != __instance.height;

            __instance.xPositionOnScreen = x;
            __instance.yPositionOnScreen = y;
            __instance.width = BarWidth;
            __instance.height = BarHeight;

            if (changed)
                ResetIcons?.Invoke(__instance, null);

            // The rectangles are written here rather than through
            // arrangeTheseComponentsInThisRectangle, which was measured leaving them
            // exactly where they were. Right to left from the right-hand edge of the
            // bar, which is how the base game lays them out.
            int slot = 0;
            foreach (ClickableTextureComponent icon in Icons(__instance))
            {
                icon.bounds = new Rectangle(
                    x + BarWidth - (slot + 1) * (IconSize + IconGap),
                    y,
                    IconSize,
                    IconSize);
                slot++;
            }

            return false;
        }

        private static IEnumerable<ClickableTextureComponent> Icons(BuffsDisplay display)
        {
            var icons = AccessTools.Field(typeof(BuffsDisplay), "buffs")
                .GetValue(display) as System.Collections.IDictionary;

            if (icons is null)
                yield break;

            foreach (System.Collections.DictionaryEntry entry in icons)
                yield return (ClickableTextureComponent)entry.Key;
        }

    }
}

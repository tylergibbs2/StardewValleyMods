using HarmonyLib;
using StardewValley;
using StardewValley.Menus;

namespace StardewRoguelike.Patches
{
    /// <summary>
    /// Lays the buff icons out along one wide row instead of wrapping them inside
    /// vanilla's 288px box, since a run accumulates a lot of curses and perks.
    ///
    /// 1.5.6 did this by widening the menu in a <c>updatePosition</c> postfix. 1.6
    /// calls <c>updatePosition</c> from <c>draw</c> every frame and derives its x
    /// from the width, so mutating those fields would make it rebuild the icons on
    /// every frame and lose the icon "pop" animation. Adjusting the layout
    /// arguments instead leaves the menu's own state alone.
    /// </summary>
    [HarmonyPatch(typeof(BuffsDisplay), nameof(BuffsDisplay.arrangeTheseComponentsInThisRectangle))]
    internal class BuffDisplayUpdatePosition
    {
        public static void Prefix(ref int rectangleX, ref int rectangleWidthInComponentWidthUnits)
        {
            rectangleX += 64;
            rectangleWidthInComponentWidthUnits = Game1.uiViewport.Width / 64;
        }
    }
}

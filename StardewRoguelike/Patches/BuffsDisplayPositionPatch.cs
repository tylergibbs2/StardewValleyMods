using HarmonyLib;
using StardewValley;
using StardewValley.Menus;

namespace StardewRoguelike.Patches
{
    /// <summary>
    /// Lays the buff icons out in the space the HUD is drawn in.
    ///
    /// BuffsDisplay places itself from GraphicsDevice.Viewport.GetTitleSafeArea(),
    /// which measures the back buffer, while the HUD is drawn in UI space. Where the
    /// two differ the icons are arranged for the wrong one: they were being put around
    /// x=1780 on a screen 1280 wide, far off the right-hand edge, which is why no
    /// curse has ever appeared in the buff bar. The adrenaline buff was going the same
    /// way, so nothing was reaching the bar at all.
    ///
    /// The base game calls this at the top of every draw, so re-placing them here is
    /// the last word before they are drawn, whichever viewport it measured.
    /// </summary>
    [HarmonyPatch(typeof(BuffsDisplay), "updatePosition")]
    internal class BuffsDisplayPositionPatch
    {
        /// <summary>The room the base game leaves on the right for the money box.</summary>
        private const int RightMargin = 300;

        /// <summary>The icons are a grid of 64px squares with an 8px gap.</summary>
        private const int IconSize = 64;
        private const int IconGap = 8;

        public static void Postfix(BuffsDisplay __instance)
        {
            __instance.xPositionOnScreen = Game1.uiViewport.Width - RightMargin - __instance.width;
            __instance.yPositionOnScreen = IconGap;

            __instance.arrangeTheseComponentsInThisRectangle(
                __instance.xPositionOnScreen,
                __instance.yPositionOnScreen,
                __instance.width / IconSize,
                IconSize,
                IconSize,
                IconGap,
                rightToLeft: true);
        }
    }
}

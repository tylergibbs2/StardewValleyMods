using HarmonyLib;
using StardewValley;
using StardewValley.Buffs;
using StardewValley.Menus;
using System.Collections.Generic;

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

        /// <summary>When the icons were last described, so it is said rarely.</summary>
        private static double lastReport;

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

            Report(__instance);
        }

        /// <summary>
        /// Says what the display is about to draw, at most once every few seconds.
        ///
        /// The icons are placed and counted but still do not appear, so this reports
        /// the components themselves - where they ended up, whether they are visible,
        /// what they are drawn from - rather than where this patch meant to put them.
        /// </summary>
        private static void Report(BuffsDisplay display)
        {
            double now = Game1.currentGameTime?.TotalGameTime.TotalSeconds ?? 0;
            if (now - lastReport < 3)
                return;

            var icons = AccessTools.Field(typeof(BuffsDisplay), "buffs")
                .GetValue(display) as System.Collections.IDictionary;

            if (icons is null || icons.Count == 0)
                return;

            lastReport = now;

            List<string> described = new();
            foreach (System.Collections.DictionaryEntry entry in icons)
            {
                ClickableTextureComponent icon = (ClickableTextureComponent)entry.Key;
                Buff buff = (Buff)entry.Value!;
                described.Add(
                    $"{buff.id} at {icon.bounds.X},{icon.bounds.Y} {icon.bounds.Width}x{icon.bounds.Height} " +
                    $"visible={icon.visible} scale={icon.scale} " +
                    $"tex={(icon.texture is null ? "null" : $"{icon.texture.Width}x{icon.texture.Height}")} " +
                    $"src={icon.sourceRect} alphaTimer={buff.displayAlphaTimer}");
            }

            ModEntry.ModMonitor.Log(
                $"buff icons: box={display.xPositionOnScreen},{display.yPositionOnScreen} " +
                $"{display.width}x{display.height} ui={Game1.uiViewport.Width}x{Game1.uiViewport.Height} :: " +
                string.Join(" :: ", described),
                StardewModdingAPI.LogLevel.Debug);
        }
    }
}

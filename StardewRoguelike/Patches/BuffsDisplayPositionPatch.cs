using HarmonyLib;
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
        private const int IconGap = 8;

        private static readonly MethodBase? ResetIcons =
            AccessTools.Method(typeof(BuffsDisplay), "resetIcons");

        /// <summary>When the icons were last described, so it is said rarely.</summary>
        private static double lastReport;

        public static bool Prefix(BuffsDisplay __instance)
        {
            int x = Game1.uiViewport.Width - RightMargin - BarWidth;
            int y = IconGap;

            if (x != __instance.xPositionOnScreen
                || y != __instance.yPositionOnScreen
                || BarWidth != __instance.width
                || BarHeight != __instance.height)
            {
                __instance.xPositionOnScreen = x;
                __instance.yPositionOnScreen = y;
                __instance.width = BarWidth;
                __instance.height = BarHeight;

                // resetIcons lays them out from the fields just set, so there is no
                // second arrangement here to disagree with it.
                ResetIcons?.Invoke(__instance, null);
            }

            Report(__instance);
            return false;
        }

        /// <summary>Says where the icons ended up, at most once every few seconds.</summary>
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
                described.Add($"{buff.id} at {icon.bounds.X},{icon.bounds.Y}");
            }

            ModEntry.ModMonitor.Log(
                $"buff icons: box={display.xPositionOnScreen},{display.yPositionOnScreen} " +
                $"{display.width}x{display.height} ui={Game1.uiViewport.Width}x{Game1.uiViewport.Height} :: " +
                string.Join(" :: ", described),
                StardewModdingAPI.LogLevel.Debug);
        }
    }
}

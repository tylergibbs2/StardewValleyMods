using HarmonyLib;
using StardewRoguelike.UI;
using StardewValley.Menus;

namespace StardewRoguelike.Patches
{
    /// <summary>
    /// Puts the run in the game menu's map tab.
    /// </summary>
    /// <remarks>
    /// The valley's map is the one page of that menu a run has no use for: the
    /// player is forty floors under it and will not be going to Pierre's. The page
    /// is swapped rather than a tab added, because adding one means laying out the
    /// tab strip, the neighbour ids that let a controller walk it, and every index
    /// in the game that refers to a tab by number.
    ///
    /// Both constructors are covered by patching this one - the other chains to it -
    /// and the patch is mode-scoped like the rest, so outside a run the map is the
    /// map again.
    /// </remarks>
    [HarmonyPatch(typeof(GameMenu), MethodType.Constructor, new[] { typeof(bool) })]
    internal class GameMenuMapPagePatch
    {
        public static void Postfix(GameMenu __instance)
        {
            if (__instance.pages.Count <= GameMenu.mapTab)
                return;

            IClickableMenu map = __instance.pages[GameMenu.mapTab];

            __instance.pages[GameMenu.mapTab] = new RunMapPage(
                map.xPositionOnScreen, map.yPositionOnScreen, map.width, map.height);
        }
    }
}

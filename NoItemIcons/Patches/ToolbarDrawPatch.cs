using HarmonyLib;
using StardewValley.Menus;

namespace NoItemIcons.Patches
{
    [HarmonyPatch(typeof(Toolbar), nameof(Toolbar.draw))]
    internal class ToolbarDrawPatch
    {
        public static bool Prefix()
        {
            return false;
        }
    }
}

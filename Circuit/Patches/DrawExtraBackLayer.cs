using HarmonyLib;
using StardewValley;
using xTile.Dimensions;

namespace Circuit.Patches
{
    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.drawBackground))]
    class DrawExtraBackLayer
    {
        public static bool Prefix(GameLocation __instance)
        {
            __instance.Map.GetLayer("BackBack")?.Draw(Game1.mapDisplayDevice, Game1.viewport, Location.Origin, wrapAround: false, 4);
            return true;
        }
    }
}

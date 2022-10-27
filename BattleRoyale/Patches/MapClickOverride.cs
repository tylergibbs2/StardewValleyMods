using StardewValley;
using StardewValley.Menus;

namespace BattleRoyale.Patches
{
    class MapClickOverride : Patch
    {
        protected override PatchDescriptor GetPatchDescriptor() => new(typeof(MapPage), "receiveLeftClick");

        public static bool Prefix(MapPage __instance, int x, int y)
        {
            foreach (var point in __instance.points)
            {
                if (point.containsPoint(x, y))
                {
                    bool r = SpectatorMode.OnClickMapPoint(point);
                    if (!r)
                        Game1.activeClickableMenu.exitThisMenu(false);
                    return r;
                }
            }

            return true;
        }
    }
}

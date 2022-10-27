using StardewValley;
using StardewValley.Locations;
using xTile.Dimensions;

namespace BattleRoyale.Patches
{
    class DrawExtraBackLayer : Patch
    {
        protected override PatchDescriptor GetPatchDescriptor() => new(typeof(GameLocation), "drawBackground");

        public static bool Prefix(GameLocation __instance)
        {
            if (__instance is not Mountain)
                return true;

            __instance.Map.GetLayer("BackBack").Draw(Game1.mapDisplayDevice, Game1.viewport, Location.Origin, wrapAround: false, 4);
            return true;
        }
    }
}

using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;

namespace BattleRoyale.Patches
{
    class BusStopFix : Patch
    {
        protected override PatchDescriptor GetPatchDescriptor() => new(typeof(BusStop), "resetLocalState");

        public static void Postfix(BusStop __instance)
        {
            ModEntry.BRGame.Helper.Reflection.GetField<Vector2>(__instance, "busPosition").SetValue(new Vector2(-1000f, -1000f));
            ModEntry.BRGame.Helper.Reflection.GetField<TemporaryAnimatedSprite>(__instance, "busDoor").SetValue(null);
        }
    }
}

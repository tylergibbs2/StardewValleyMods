using StardewValley;
using StardewValley.Locations;

namespace BattleRoyale.Patches
{
    class DisableSafariGuy : Patch
    {
        protected override PatchDescriptor GetPatchDescriptor() => new(typeof(IslandFieldOffice), "resetLocalState");

        public static bool Prefix(IslandFieldOffice __instance)
        {
            ModEntry.BRGame.Helper.Reflection.GetField<NPC>(__instance, "safariGuy").SetValue(null);
            return false;
        }
    }
}

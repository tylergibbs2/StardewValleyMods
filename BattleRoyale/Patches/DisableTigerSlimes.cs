using Netcode;
using StardewValley.Locations;

namespace BattleRoyale.Patches
{
    class DisableTigerSlimes : Patch
    {
        protected override PatchDescriptor GetPatchDescriptor() => new(typeof(IslandWest), "resetSharedState");

        public static bool Prefix(IslandWest __instance)
        {
            NetBool addedSlimes = ModEntry.BRGame.Helper.Reflection.GetField<NetBool>(__instance, "addedSlimesToday").GetValue();
            addedSlimes.Value = true;
            return true;
        }
    }
}

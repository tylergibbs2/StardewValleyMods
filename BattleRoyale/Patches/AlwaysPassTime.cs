using StardewValley;

namespace BattleRoyale.Patches
{
    class AlwaysPassTime : Patch
    {
        protected override PatchDescriptor GetPatchDescriptor() => new(typeof(Game1), "shouldTimePass");

        public static bool Prefix(ref bool __result)
        {
            __result = true;
            return false;
        }
    }
}

using StardewValley;

namespace BattleRoyale.Patches
{
    class DisableToolbarShift : Patch
    {
        protected override PatchDescriptor GetPatchDescriptor() => new(typeof(Farmer), "shiftToolbar");

        public static bool Prefix()
        {
            return false;
        }
    }
}

using StardewValley;

namespace BattleRoyale.Patches
{
    class EventDisabler : Patch
    {
        protected override PatchDescriptor GetPatchDescriptor() => new(typeof(GameLocation), "checkForEvents");

        public static bool Prefix()
        {
            return false;
        }
    }
}

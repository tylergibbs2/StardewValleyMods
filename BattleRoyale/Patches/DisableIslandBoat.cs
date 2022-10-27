using StardewValley;
using StardewValley.Locations;

namespace BattleRoyale.Patches
{
    class DisableIslandBoat : Patch
    {
        protected override PatchDescriptor GetPatchDescriptor() => new(typeof(IslandSouth), "performTouchAction");

        public static bool Prefix(string fullActionString)
        {
            if (Game1.eventUp)
                return true;

            string action = fullActionString.Split(' ')[0];

            if (action == "LeaveIsland")
                return false;

            return true;
        }
    }
}

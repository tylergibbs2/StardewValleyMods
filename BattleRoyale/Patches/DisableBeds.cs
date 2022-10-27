using StardewValley;

namespace BattleRoyale.Patches
{
    class DisableBeds : Patch
    {
        protected override PatchDescriptor GetPatchDescriptor() => new(typeof(GameLocation), "performTouchAction");

        public static bool Prefix(string fullActionString)
        {
            if (Game1.eventUp)
                return true;

            string action = fullActionString.Split(' ')[0];

            if (action == "Sleep")
                return false;

            return true;
        }
    }
}

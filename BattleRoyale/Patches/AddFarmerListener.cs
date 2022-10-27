using StardewValley;
using StardewValley.Network;

namespace BattleRoyale.Patches
{
    class AddFarmerListener : Patch
    {
        //runs when a client joins
        protected override PatchDescriptor GetPatchDescriptor() => new(typeof(Multiplayer), "addPlayer");

        public static bool Prefix(NetFarmerRoot f)
        {
            if (Game1.IsServer)
            {
                ModEntry.BRGame.ProcessPlayerJoin(f);

                return AutoKicker.ProcessPlayerJoin(f);
            }

            return true;
        }
    }
}

using BattleRoyale.Utils;
using StardewValley;
using System.Collections.Generic;

namespace BattleRoyale.Patches
{
    class PlayerDisconnectListener : Patch
    {
        protected override PatchDescriptor GetPatchDescriptor() => new(typeof(Multiplayer), "playerDisconnected");

        public static bool Prefix(long id, List<long> ___disconnectingFarmers)
        {
            Round round = ModEntry.BRGame.GetActiveRound();
            if (Game1.IsServer && Game1.otherFarmers.ContainsKey(id) && !___disconnectingFarmers.Contains(id))
                round?.HandleDeath(DamageSource.WORLD, Game1.otherFarmers[id]);

            return true;
        }
    }
}

using BattleRoyale.Utils;
using StardewValley;
using System;

namespace BattleRoyale.Patches
{
    class WarpFarmerListener : Patch
    {
        protected override PatchDescriptor GetPatchDescriptor() => new(typeof(Game1), "warpFarmer", new Type[] { typeof(LocationRequest), typeof(int), typeof(int), typeof(int) });

        public static bool Prefix(ref LocationRequest locationRequest)
        {
            FarmerUtils.AddKnockbackImmunity();

            if (!SpectatorMode.InSpectatorMode)
            {
                if (locationRequest.Location.Name == "Tunnel")
                {
                    var m = Game1.player.mount;
                    Game1.player.mount = null;
                    Game1.warpFarmer("Desert", 46, 27, false);
                    Game1.player.mount = m;

                    return false;
                }
            }

            return true;
        }
    }
}

using BattleRoyale.Utils;
using StardewValley;
using System;

namespace BattleRoyale.Patches
{
    class HospitalEventRemover : Patch
    {
        protected override PatchDescriptor GetPatchDescriptor() => new(typeof(Farmer), "Update");

        public static void Prefix(Farmer __instance)
        {
            if (__instance == Game1.player && __instance.health <= 0)
            {
                Console.WriteLine("Killed by internal game event, e.g. explosion");
                FarmerUtils.TakeDamage(Game1.player, DamageSource.WORLD, 100);
            }
        }
    }
}

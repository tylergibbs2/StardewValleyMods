﻿using Microsoft.Xna.Framework;
using Netcode;
using StardewValley;
using StardewValley.Projectiles;
using System;
using System.Linq;

namespace BattleRoyale.Patches
{
    class SlingshotPatch3 : Patch
    {
        protected override PatchDescriptor GetPatchDescriptor() => new(typeof(Projectile), "isColliding");

        public static bool Postfix(bool __result, Projectile __instance, GameLocation location)
        {
            bool damagesMonsters = ModEntry.BRGame.Helper.Reflection.GetField<NetBool>(__instance, "damagesMonsters").GetValue().Value;

            if (!__result && (__instance is not BasicProjectile bp || bp.damageToFarmer.Value < 20))
            {
                Rectangle r = __instance.getBoundingBox();
                foreach (Farmer farmer in location.farmers.Where(x => x != null))
                {
                    var bounds = SlingshotPatch5.GetFarmerBounds(farmer);

                    if (bounds.Intersects(r))
                    {
                        Console.WriteLine("Collide with farmer");
                        //__result = true;
                        return true;
                    }
                }
            }

            if (!__result)
            {
                //  x / Game1.tileSize, y / Game1.tileSize
                var center = __instance.getBoundingBox().Center;
                var centerTile = new Vector2((center.X / Game1.tileSize), (center.Y / Game1.tileSize));
                if (location.objects.TryGetValue(centerTile, out StardewValley.Object obj) && obj.Name.ToLower().Contains("fence"))
                    return true;
            }

            return __result;
        }

    }
}

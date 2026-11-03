using HarmonyLib;
using Microsoft.Xna.Framework;
using Netcode;
using StardewValley;
using StardewValley.Network;
using StardewValley.Projectiles;

namespace StardewRoguelike.Patches
{
    [HarmonyPatch(typeof(Projectile), "isColliding")]
    internal class ProjectilePassableTileProperty
    {
        public static bool Prefix(ref bool __result, Projectile __instance, GameLocation location)
        {
			NetPosition position = __instance.position;
			NetBool damagesMonsters = __instance.damagesMonsters;

			Vector2 tilePosition = new(position.Value.X / 64f, position.Value.Y / 64f);
			if (location.isTileOnMap(position.Value / 64f) && location.doesTileHaveProperty((int)tilePosition.X, (int)tilePosition.Y, "ProjectilePassable", "Buildings") is not null)
			{
				if (damagesMonsters.Value && location.doesPositionCollideWithCharacter(__instance.getBoundingBox()) != null)
                {
                    __result = true;
                    return false;
                }
                __result = false;
				return false;
			}

			return true;
        }
    }
}

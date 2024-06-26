using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;
using System;
using System.Reflection;

namespace StardewRoguelike.Patches
{
    [HarmonyPatch(typeof(MineShaft), nameof(MineShaft.tryToAddOreClumps))]
    internal class OreClumpsPatch
    {
		public static bool Prefix(MineShaft __instance)
        {
			Random mineRandom = (Random)__instance.GetType().GetField("mineRandom", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(__instance)!;

			if (!(mineRandom.NextDouble() < 0.55))
				return false;

			Vector2 endPoint = __instance.getRandomTile();
			for (int tries = 0; tries < 1 || mineRandom.NextDouble() < 0.25; tries++)
			{
				if (__instance.isTileLocationTotallyClearAndPlaceable(endPoint) && __instance.isTileOnClearAndSolidGround(endPoint) && __instance.doesTileHaveProperty((int)endPoint.X, (int)endPoint.Y, "Diggable", "Back") is null)
				{
                    SObject ore = new(endPoint, 764, "Stone", canBeSetDown: true, canBeGrabbed: false, isHoedirt: false, isSpawnedObject: false);
					ore.MinutesUntilReady = 8;
					Utility.recursiveObjectPlacement(ore, (int)endPoint.X, (int)endPoint.Y, 0.949999988079071, 0.30000001192092896, __instance, "Dirt", (ore.ParentSheetIndex == 668) ? 1 : 0, 0.05000000074505806, (ore.ParentSheetIndex != 668) ? 1 : 2);
				}
				endPoint = __instance.getRandomTile();
			}

			return false;
		}
    }
}

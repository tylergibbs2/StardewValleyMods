using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewRoguelike.Run;
using StardewValley;
using StardewValley.Locations;
using System;

namespace StardewRoguelike.Patches
{
    [HarmonyPatch(typeof(MineShaft), nameof(MineShaft.tryToAddOreClumps))]
    internal class OreClumpsPatch
    {
		public static bool Prefix(MineShaft __instance)
        {
			FloorKind kind = RunRoute.KindFor(Roguelike.GetLevelFromMineshaft(__instance));

			// A shrine floor is a fight rather than a dig. This is a separate entry
			// point from populateLevel, so zeroing the stone chances there left the
			// gold standing.
			if (kind == FloorKind.Shrine)
				return false;

			Random mineRandom = __instance.mineRandom;

			// Rolled either way, so that taking a dark mineshaft does not shift
			// everything the mine draws afterwards.
			bool rolled = mineRandom.NextDouble() < 0.55;

			// A dark mineshaft always has its gold. It is the floor people take for
			// the ore, so leaving it to a coin toss would make the choice a gamble
			// rather than a trade.
			if (!rolled && kind != FloorKind.DarkMineshaft)
				return false;

			Vector2 endPoint = __instance.getRandomTile();
			for (int tries = 0; tries < 1 || mineRandom.NextDouble() < 0.25; tries++)
			{
				if (__instance.CanItemBePlacedHere(endPoint) && __instance.isTileOnClearAndSolidGround(endPoint) && __instance.doesTileHaveProperty((int)endPoint.X, (int)endPoint.Y, "Diggable", "Back") is null)
				{
                    SObject ore = new("764", 1) { TileLocation = endPoint };
					ore.MinutesUntilReady = 8;
					Utility.recursiveObjectPlacement(ore, (int)endPoint.X, (int)endPoint.Y, 0.949999988079071, 0.30000001192092896, __instance, "Dirt", (ore.ParentSheetIndex == 668) ? 1 : 0, 0.05000000074505806, (ore.ParentSheetIndex != 668) ? 1 : 2);
				}
				endPoint = __instance.getRandomTile();
			}

			return false;
		}
    }
}

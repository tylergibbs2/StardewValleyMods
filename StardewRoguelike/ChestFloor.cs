﻿using StardewRoguelike.Extensions;
using StardewRoguelike.VirtualProperties;
using StardewValley.Locations;

namespace StardewRoguelike
{
    public class ChestFloor
    {
        public static readonly string ChestFloorMapPath = "10";

        public static readonly double ChestFloorChance = 0.025;

        /// <summary>
        /// Spawns a chest for all players in a specified MineShaft.
        /// The chest contains a random item from the *next* merchant.
        /// </summary>
        /// <param name="mine">The mine.</param>
        public static void SpawnChest(MineShaft mine)
        {
            mine.SpawnLocalChest(new(9, 9));
        }

        /// <summary>
        /// Checks whether a specified level should be a chest floor.
        /// Two chest floors cannot appear back to back. Otherwise,
        /// calculation is random chance.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <returns>true if we should, false otherwise.</returns>
        public static bool ShouldDoChestFloor(int level)
        {
            if (DebugCommands.ForcedChestFloor)
                return true;

            if (level > 6 && level < 12)
                return false;

            bool previousFloorWasChestFloor = false;
            foreach (MineShaft mine in MineShaft.activeMines)
            {
                if (Roguelike.GetLevelFromMineshaft(mine) == level - 1)
                {
                    previousFloorWasChestFloor = IsChestFloor(mine);
                    break;
                }
            }

            return Roguelike.FloorRng.NextDouble() <= ChestFloorChance && !previousFloorWasChestFloor;
        }

        /// <summary>
        /// Checks whether a specified MineShaft is a chest floor.
        /// This data is stored internally on the MineShaft instance.
        /// </summary>
        /// <param name="mine">The mine.</param>
        /// <returns>true if it is, false otherwise.</returns>
        public static bool IsChestFloor(MineShaft mine)
        {
            return mine.get_MineShaftChestFloor().Value;
        }
    }
}

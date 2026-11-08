using StardewRoguelike.Extensions;
using StardewValley;
using StardewValley.Objects;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
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
            int level = Roguelike.GetLevelFromMineshaft(mine);
            Item? item = ItemFor(level);

            // Handed the item rather than left to pick one, so the chest holds what
            // the exit menu said it held. A chest that rolled its own contents on
            // arrival could not be named from the floor above.
            if (item is null)
                mine.SpawnLocalChest(new(9, 9));
            else
                mine.SpawnLocalChest(new(9, 9), item);
        }

        /// <summary>
        /// What a plain chest on <paramref name="level"/> is holding, for this player.
        /// </summary>
        /// <remarks>
        /// Worked out from the seed, so asking twice gives the same answer and the
        /// exit can say what is down there. What the player already carries is still
        /// struck off, which is theirs alone - two players opening one chest are owed
        /// different things, the same way they are at a shrine.
        /// </remarks>
        public static Item? ItemFor(int level)
        {
            List<string> carried = new();

            foreach (Item item in Game1.player.Items)
            {
                if (item is MeleeWeapon or Ring or Boots)
                    carried.Add(item.DisplayName);
            }

            if (Game1.player.boots.Value is not null)
                carried.Add(Game1.player.boots.Value.DisplayName);
            if (Game1.player.leftRing.Value is not null)
                carried.Add(Game1.player.leftRing.Value.DisplayName);
            if (Game1.player.rightRing.Value is not null)
                carried.Add(Game1.player.rightRing.Value.DisplayName);

            Random random = Run.RunPlan.ChestRandomFor(level);

            return Merchant.GetChestMerchantFloor(level, random)
                .PickAnyRandomAvoiding(carried, random);
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

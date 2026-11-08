using StardewRoguelike.Extensions;
using StardewValley;
using StardewValley.Objects;
using StardewValley.Enchantments;
using StardewRoguelike.Enchantments;
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
            // Left indeterminate on purpose. Each player works the offer out on
            // their own machine, from the same seed but against their own
            // inventory, so two players at one chest are owed different things -
            // and the exit menu, which is also local, names what that player will
            // actually find.
            mine.SpawnLocalChest(new(9, 9));
        }

        /// <summary>What a treasure chest on a level holds, and what it wants for it.</summary>
        public readonly record struct Offer(Item? Item, ChestPrice Price, int Gold);

        /// <summary>
        /// The offer a plain treasure chest on <paramref name="level"/> is making.
        /// </summary>
        /// <remarks>
        /// Always from the shelf ahead, so it is always worth having. What varies is
        /// the price and whether the thing is cursed, which is what makes opening it
        /// a decision - a chest that merely rolled a quality was either a certainty
        /// or a disappointment, and neither is worth walking a floor for.
        ///
        /// Worked out from the seed, so asking twice gives the same answer and the
        /// floor above can say what is waiting.
        /// </remarks>
        public static Offer OfferFor(int level)
        {
            Random random = Run.RunPlan.ChestRandomFor(level);

            ChestPrice price = (ChestPrice)random.Next(3);
            int gold = GoldPrice(level, random);

            List<string> carried = new();

            foreach (Item held in Game1.player.Items)
            {
                if (held is MeleeWeapon or Ring or Boots)
                    carried.Add(held.DisplayName);
            }

            if (Game1.player.boots.Value is not null)
                carried.Add(Game1.player.boots.Value.DisplayName);
            if (Game1.player.leftRing.Value is not null)
                carried.Add(Game1.player.leftRing.Value.DisplayName);
            if (Game1.player.rightRing.Value is not null)
                carried.Add(Game1.player.rightRing.Value.DisplayName);

            Item item = Merchant.GetNextMerchantFloor(level).PickAnyRandomAvoiding(carried, random);

            Curse(item, random);

            return new Offer(item, price, gold);
        }

        /// <summary>How likely a weapon in a chest comes with something on it.</summary>
        private const double CurseChance = 0.4;

        /// <summary>Puts one of the cursed enchantments on a weapon, sometimes.</summary>
        private static void Curse(Item item, Random random)
        {
            if (item is not MeleeWeapon weapon || random.NextDouble() >= CurseChance)
                return;

            BaseEnchantment enchantment = random.Next(3) switch
            {
                0 => new HeavyEnchantment(),
                1 => new HollowEnchantment(),
                _ => new BrittleEnchantment()
            };

            weapon.AddEnchantment(enchantment);
        }

        /// <summary>What a chest asks in gold, which grows with the shelf it draws from.</summary>
        private static int GoldPrice(int level, Random random)
        {
            int tier = (level / 6) + 1;

            return 250 * tier + random.Next(0, 3) * 50;
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

using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Enchantments;
using StardewValley.Monsters;
using System;

namespace StardewRoguelike.Patches
{
    [HarmonyPatch(typeof(VampiricEnchantment), nameof(VampiricEnchantment.OnMonsterSlay))]
    internal class VampiricEnchantmentNerf
    {
        public static bool Prefix(Monster monster, GameLocation location, Farmer who)
        {
            if (Game1.random.NextDouble() < 0.1)
            {
                int amount = Math.Max(1, (int)Math.Min((monster.MaxHealth + Game1.random.Next(-monster.MaxHealth / 10, monster.MaxHealth / 15 + 1)) * 0.1f, 30));
                // Through the budget, so a cascade of kills cannot roll this once per
                // corpse and heal through anything.
                amount = Sustain.Heal(amount);

                if (amount == 0)
                    return false;

                location.debris.Add(new Debris(amount, new Vector2(Game1.player.StandingPixel.X, Game1.player.StandingPixel.Y), Color.Lime, 1f, who));
                Game1.playSound("healSound");
            }

            return false;
        }
    }
}

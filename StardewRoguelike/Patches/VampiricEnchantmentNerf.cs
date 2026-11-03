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
                who.health = Math.Min(who.maxHealth, Game1.player.health + amount);
                location.debris.Add(new Debris(amount, new Vector2(Game1.player.getStandingX(), Game1.player.getStandingY()), Color.Lime, 1f, who));
                Game1.playSound("healSound");
            }

            return false;
        }
    }
}

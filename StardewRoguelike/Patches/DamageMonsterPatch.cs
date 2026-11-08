using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using System;

namespace StardewRoguelike.Patches
{
    [HarmonyPatch(
        typeof(GameLocation),
        "damageMonster",
        // 1.6 appended isProjectile; without it the overload no longer resolves.
        new Type[] { typeof(Rectangle), typeof(int), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(float), typeof(float), typeof(bool), typeof(Farmer), typeof(bool) })
    ]
    internal class DamageMonsterPatch
    {
        public static bool Prefix(ref int minDamage, ref int maxDamage, ref float knockBackModifier, ref float critChance, ref float critMultiplier)
        {
            Keystones.ModifyOutgoingDamage(ref minDamage, ref maxDamage);

            critMultiplier *= (float)Math.Pow(1.2f, Perks.PerkLevel(Perks.PerkType.Precision));
            critMultiplier += 1.5f * Perks.PerkLevel(Perks.PerkType.Desperado);

            if (Curse.HasCurse(CurseType.PlayerKnockback))
                knockBackModifier *= 2;

            knockBackModifier = Math.Min(knockBackModifier, 2f);

            if (Curse.HasCurse(CurseType.MoreCritsLessDamage))
            {
                critChance = 0.25f;
                critMultiplier *= 0.5f;
            }

            return true;
        }

        public static void Postfix(bool __result, int minDamage, int maxDamage)
        {
            if (__result)
            {
                // A blow that landed, which is what a flurry counts.
                Keystones.OnMonsterHit();

                if (Curse.HasCurse(CurseType.BrittleCrown))
                {
                    int avg = (maxDamage + minDamage) / 2;
                    Game1.player.Money += Math.Max(1, avg / 10);
                    if (Game1.random.NextDouble() < 0.25)
                        Game1.playSound("coin");
                }

                // The chance rises with the level rather than the amount healed, so
                // it reads as happening more often rather than harder.
                int leech = Perks.PerkLevel(Perks.PerkType.Leech);
                if (leech > 0 && Game1.random.NextDouble() < 0.25 * leech)
                    Game1.player.health = Math.Min(Game1.player.health + 1, Game1.player.maxHealth);
            }
        }
    }
}

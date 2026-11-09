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
        /// <summary>What one level of Precision adds to a critical hit's multiplier.</summary>
        private const float PrecisionCritPower = 0.4f;

        /// <summary>The same for Desperado, which is the rarer and stronger of the two.</summary>
        private const float DesperadoCritPower = 0.8f;

        /// <summary>The most a critical hit may ever multiply a blow by.</summary>
        /// <remarks>
        /// Twice what the game's own weapons crit for. A weapon's multiplier is three,
        /// and the best enchantment in the game lifts it to three and a half, so six
        /// is already generous for two perks - it is a ceiling, not a target, and it
        /// exists so that the answer to "how hard can a crit hit" is a number somebody
        /// chose rather than whatever this method's arithmetic happened to produce.
        /// </remarks>
        private const float CritMultiplierCap = 6f;

        public static bool Prefix(ref int minDamage, ref int maxDamage, ref float knockBackModifier, ref float critChance, ref float critMultiplier)
        {
            Keystones.ModifyOutgoingDamage(ref minDamage, ref maxDamage);

            // One pool, then one ceiling. Precision used to multiply the multiplier
            // while Desperado added to it, so the two composed rather than stacked:
            // three levels of each took a weapon's x3 to x9.7. That number then
            // multiplied everything downstream of it, keystones included, which is
            // what made two perks nobody had touched the largest term in the worst
            // case the keystones were being blamed for.
            //
            // Additive into a shared cap, because a cap on its own would have been
            // the wrong fix: Desperado alone reached x7.5, so clamping without
            // reworking the coefficients would have left its third level - and all of
            // Precision alongside it - doing nothing whatever. This way every level
            // of both is always worth taking, and the pair has one legible promise: a
            // crit is never worth more than six ordinary hits.
            float critPower =
                PrecisionCritPower * Perks.PerkLevel(Perks.PerkType.Precision)
                + DesperadoCritPower * Perks.PerkLevel(Perks.PerkType.Desperado);

            critMultiplier = Math.Min(critMultiplier + critPower, CritMultiplierCap);

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

        public static void Postfix(GameLocation __instance, Rectangle areaOfEffect, bool __result, int minDamage, int maxDamage)
        {
            if (__result)
            {
                // A blow a keystone spread onto a neighbour is not a blow the player
                // struck, and nothing below here should count it. Everything in this
                // method is written per swing - a leech roll, a crown payout - and a
                // sweep that hits five monsters would otherwise run all of it five
                // times.
                if (Keystones.IsSpreading)
                    return;

                // A blow that landed, which is what a flurry counts. The area is
                // handed over with it, since the tiers that spread a blow onto the
                // target's neighbours have no other way to know where it landed.
                Keystones.OnMonsterHit(__instance, areaOfEffect, minDamage, maxDamage);

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
                    Sustain.Heal(1);
            }
        }
    }
}

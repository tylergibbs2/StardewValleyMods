using HarmonyLib;
using StardewValley;
using StardewValley.Buffs;
using System.Linq;

namespace StardewRoguelike.Patches
{
    /// <summary>
    /// Makes food and drink buffs last the whole run instead of expiring.
    ///
    /// 1.5.6 had separate <c>BuffsDisplay.tryToAddFoodBuff</c>/<c>tryToAddDrinkBuff</c>
    /// methods to hook; 1.6 routes every buff through the player's buff manager,
    /// so there is one seam for both.
    /// </summary>
    [HarmonyPatch(typeof(BuffManager), nameof(BuffManager.Apply))]
    internal class PermanentBuffPatch
    {
        public static void Prefix(Buff buff)
        {
            // Curses manage their own duration, and anything that isn't from eating
            // or drinking (debuffs, speed pads, the adrenaline buff) keeps its timer.
            if (buff is Curse || !Curse.FoodBuffIds.Contains(buff.id))
                return;

            if (Curse.HasCurse(CurseType.BuffsMorePotent))
            {
                Curse.MakePotent(buff);
                return;
            }

            buff.millisecondsDuration = Buff.ENDLESS;
            buff.totalMillisecondsDuration = Buff.ENDLESS;
        }
    }

    /// <summary>
    /// Keeps the extra effects the roguelike attaches to drinking:
    /// alcohol also makes you tipsy, and Life Elixir heals to full
    /// (or feeds the heal-over-time curse).
    /// </summary>
    [HarmonyPatch(typeof(BuffManager), nameof(BuffManager.Apply))]
    internal class DrinkBuffSideEffectsPatch
    {
        public static void Postfix(Buff buff)
        {
            if (buff is Curse || buff.id != "drink" || buff.source is null)
                return;

            if (buff.source.Contains("Beer") || buff.source.Contains("Wine") || buff.source.Contains("Mead") || buff.source.Contains("Pale Ale"))
            {
                Game1.player.applyBuff(new Buff(Buff.tipsy, duration: buff.millisecondsDuration));
            }
            else if (buff.source.Equals("Life Elixir"))
            {
                if (Curse.HasCurse(CurseType.HealOverTime))
                    Curse.HOTHealToTick += (int)(Game1.player.maxHealth * 1.5);
                else
                    Game1.player.health = Game1.player.maxHealth;
            }
        }
    }
}

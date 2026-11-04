using HarmonyLib;
using StardewRoguelike.HatQuests;
using StardewRoguelike.VirtualProperties;
using StardewValley;
using System;

namespace StardewRoguelike.Patches
{
    /// <summary>
    /// Applies the roguelike's eating bonuses on top of vanilla's.
    ///
    /// 1.5.6 needed a full reimplementation of <c>Farmer.doneEating</c>, because the
    /// buff and healing logic was inlined there and read <c>Data/ObjectInformation</c>
    /// strings directly. 1.6 moved all of that out: food buffs come from
    /// <c>Item.GetFoodOrDrinkBuffs()</c>, the nausea removal for ginger is vanilla
    /// behaviour, and healing goes through
    /// <see cref="StardewValley.Object.healthRecoveredOnConsumption"/>. So the mod
    /// only needs to adjust the heal amount and redirect it when the heal-over-time
    /// curse is active.
    /// </summary>
    [HarmonyPatch(typeof(SObject), nameof(SObject.healthRecoveredOnConsumption))]
    internal class FoodHealAmountPatch
    {
        public static void Postfix(ref int __result)
        {
            if (__result <= 0)
                return;

            if (Perks.HasPerk(Perks.PerkType.FoodEnjoyer))
                __result = (int)(__result * 1.2f);

            if (HatQuest.HasBuffFor(HatQuestType.CHEF_HAT))
                __result = (int)(__result * 1.2f);
        }
    }

    /// <summary>
    /// Routes health gained from eating into the heal-over-time curse rather than
    /// applying it instantly, and credits it to the active hat quest.
    /// </summary>
    [HarmonyPatch(typeof(Farmer), nameof(Farmer.doneEating))]
    internal class FarmerDoneEatingPatch
    {
        private static int healthBeforeEating;

        public static void Prefix(Farmer __instance)
        {
            healthBeforeEating = __instance.health;
        }

        public static void Postfix(Farmer __instance)
        {
            if (__instance != Game1.player)
                return;

            int healed = __instance.health - healthBeforeEating;
            if (healed <= 0)
                return;

            if (Curse.HasCurse(CurseType.HealOverTime))
            {
                // Take the instant heal back and pay it out over time instead.
                __instance.health = healthBeforeEating;
                Curse.HOTHealToTick += (int)Math.Round(healed * 1.5f);
                return;
            }

            if (__instance.get_FarmerActiveHatQuest() is not null)
                __instance.get_FarmerActiveHatQuest()!.HealthHealed += healed;
        }
    }
}

using HarmonyLib;
using StardewValley;
using System;

namespace StardewRoguelike.Patches
{
    [HarmonyPatch(typeof(Farmer), nameof(Farmer.reduceActiveItemByOne))]
    internal class SnakeMilkDrinkPatch
    {
        public static bool Prefix(Farmer __instance)
        {
            if (__instance.CurrentItem is not null && __instance.CurrentItem.ParentSheetIndex == 803)
            {
                int toAdd = 25;

                Roguelike.TrueMaxHP = Math.Min(Roguelike.TrueMaxHP + toAdd, Roguelike.MaxHP + (Perks.HasPerk(Perks.PerkType.Defender) ? 25 : 0));

                if (Curse.HasCurse(CurseType.GlassCannon))
                    Game1.player.health = Math.Min(Game1.player.health + toAdd, Roguelike.TrueMaxHP / 2);
                else
                    Game1.player.health = Math.Min(Game1.player.health + toAdd, Roguelike.TrueMaxHP);
            }

            return true;
        }
    }
}

﻿using StardewValley.Monsters;
using StardewValley;
using StardewRoguelike.Bosses;

namespace StardewRoguelike.Enchantments
{
    public class CustomBugKillerEnchantment : BaseWeaponEnchantment
    {
        protected override void _OnDealDamage(Monster monster, GameLocation location, Farmer who, ref int amount)
        {
            if (monster is Grub || monster is Fly || monster is Bug || monster is Leaper || monster is LavaCrab || monster is RockCrab)
                amount = (int)(amount * 2f);

            if (monster is IBossMonster boss && boss.DisplayName.Contains("Odys"))
                amount = (int)(amount * 2f);
        }

        public override string GetName()
        {
            return "Bug Killer";
        }
    }
}

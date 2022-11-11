using System;
using StardewRoguelike.HatQuests;
using StardewRoguelike.VirtualProperties;
using StardewValley;
using StardewValley.Monsters;

namespace StardewRoguelike.Enchantments
{
    public class WeaponStatTrack : BaseEnchantment
    {
        protected override void _OnDealDamage(Monster monster, GameLocation location, Farmer who, ref int amount)
        {
            ModEntry.ActiveStats.DamageDealt += amount / 2;

            if (who != Game1.player)
                return;

            if (who.get_FarmerActiveHatQuest() is not null)
                who.get_FarmerActiveHatQuest()!.DamageDealt += amount / 2;

            if (HatQuest.HasBuffFor(HatQuestType.WARRIOR_HELM))
                amount = (int)Math.Round(amount * 1.2f);

        }

        protected override void _OnMonsterSlay(Monster monster, GameLocation location, Farmer who)
        {
            ModEntry.ActiveStats.MonstersKilled++;
        }
    }
}

using System;
using StardewRoguelike.HatQuests;
using StardewRoguelike.VirtualProperties;
using StardewValley;
using StardewValley.Enchantments;
using StardewValley.Monsters;

namespace StardewRoguelike.Enchantments
{
    public class WeaponStatTrack : BaseEnchantment
    {
        public override void OnCalculateDamage(Monster monster, GameLocation location, Farmer who, bool fromBomb, ref int amount)
        {
            if (who != Game1.player)
                return;

            if (HatQuest.HasBuffFor(HatQuestType.WARRIOR_HELM))
                amount = (int)Math.Round(amount * 1.2f);
        }

        public override void OnDealtDamage(Monster monster, GameLocation location, Farmer who, bool fromBomb, int amount)
        {
            ModEntry.ActiveStats.DamageDealt += amount;

            if (who != Game1.player)
                return;

            if (who.get_FarmerActiveHatQuest() is not null)
                who.get_FarmerActiveHatQuest()!.DamageDealt += amount;
        }

        public override void OnMonsterSlay(Monster monster, GameLocation location, Farmer who, bool slainByBomb)
        {
            ModEntry.ActiveStats.MonstersKilled++;
        }
    }
}

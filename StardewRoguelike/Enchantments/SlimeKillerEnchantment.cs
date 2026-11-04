using StardewValley.Monsters;
using StardewValley;
using StardewValley.Enchantments;

namespace StardewRoguelike.Enchantments
{
    public class SlimeKillerEnchantment : BaseWeaponEnchantment
    {
        public override void OnCalculateDamage(Monster monster, GameLocation location, Farmer who, bool fromBomb, ref int amount)
        {
            if (monster is GreenSlime || monster is BigSlime)
                amount = (int)(amount * 2f);
        }

        public override string GetName()
        {
            return I18n.Enchantments_SlimeKiller();
        }
    }
}

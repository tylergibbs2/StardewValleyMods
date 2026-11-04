using StardewValley;
using StardewValley.Enchantments;
using StardewValley.Monsters;

namespace StardewRoguelike.Enchantments
{
    public class GlassCannonEnchantment : BaseEnchantment
    {
        public override void OnCalculateDamage(Monster monster, GameLocation location, Farmer who, bool fromBomb, ref int amount)
        {
            amount *= 2;
        }
    }
}

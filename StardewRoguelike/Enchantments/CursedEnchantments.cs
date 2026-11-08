using StardewValley;
using StardewValley.Enchantments;
using StardewValley.Monsters;
using StardewValley.Tools;

namespace StardewRoguelike.Enchantments
{
    /// <summary>
    /// A weapon that is worth more than it should be, and takes something for it.
    ///
    /// Treasure used to be a question of quality - was the thing in the box better
    /// than the thing in your hand - and the answer was always yes, because the box
    /// drew from a shelf above you and struck out what you already carried. Widening
    /// the shelf only replaced a certain yes with a sometimes-nothing, and a sword
    /// you will not equip is not a decision, it is a wasted floor.
    ///
    /// These are the other answer. A cursed weapon is strong enough to want at any
    /// point in a run and awkward enough to think about, so the question stops being
    /// "is this better" and becomes "can I live with this".
    /// </summary>
    public abstract class CursedEnchantment : BaseEnchantment
    {
        /// <summary>What the weapon hits for, as a multiple of what it would.</summary>
        protected abstract float DamageMultiplier { get; }

        public override void OnCalculateDamage(Monster monster, GameLocation location, Farmer who, bool fromBomb, ref int amount)
        {
            amount = (int)(amount * DamageMultiplier);
        }

        public override bool CanApplyTo(Item item)
        {
            return item is MeleeWeapon;
        }

        public override bool ShouldBeDisplayed()
        {
            return true;
        }
    }

    /// <summary>Hits far harder and swings far slower.</summary>
    public class HeavyEnchantment : CursedEnchantment
    {
        private const int SpeedCost = 3;

        protected override float DamageMultiplier => 1.7f;

        public override string GetName() => "Heavy";

        protected override void _ApplyTo(Item item)
        {
            base._ApplyTo(item);

            if (item is MeleeWeapon weapon)
                weapon.speed.Value -= SpeedCost;
        }

        protected override void _UnapplyTo(Item item)
        {
            base._UnapplyTo(item);

            if (item is MeleeWeapon weapon)
                weapon.speed.Value += SpeedCost;
        }
    }

    /// <summary>
    /// Hits twice as hard and will not push anything away from you.
    ///
    /// The nastiest of the three in practice: knockback is most of what keeps a
    /// crowd off a player, and a weapon with none of it changes how a floor is
    /// fought rather than how quickly it ends.
    /// </summary>
    public class HollowEnchantment : CursedEnchantment
    {
        protected override float DamageMultiplier => 2f;

        public override string GetName() => "Hollow";

        /// <summary>What the weapon's knockback was, so it can be handed back.</summary>
        private float taken;

        protected override void _ApplyTo(Item item)
        {
            base._ApplyTo(item);

            if (item is MeleeWeapon weapon)
            {
                taken = weapon.knockback.Value;
                weapon.knockback.Value = 0f;
            }
        }

        protected override void _UnapplyTo(Item item)
        {
            base._UnapplyTo(item);

            if (item is MeleeWeapon weapon)
                weapon.knockback.Value = taken;
        }
    }

    /// <summary>
    /// Hits harder, and everything that reaches you hits harder too.
    ///
    /// Read in FarmerTakeDamagePatch rather than here, since what it costs happens
    /// to the player rather than to the weapon.
    /// </summary>
    public class BrittleEnchantment : CursedEnchantment
    {
        /// <summary>How much more of everything the player takes while holding it.</summary>
        public const float DamageTakenMultiplier = 1.3f;

        protected override float DamageMultiplier => 1.6f;

        public override string GetName() => "Brittle";

        /// <summary>Whether the player is holding a weapon with this on it.</summary>
        public static bool IsHeld()
        {
            if (Game1.player.CurrentTool is not MeleeWeapon weapon)
                return false;

            foreach (BaseEnchantment enchantment in weapon.enchantments)
            {
                if (enchantment is BrittleEnchantment)
                    return true;
            }

            return false;
        }
    }
}

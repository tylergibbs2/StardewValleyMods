using System.Collections.Generic;

namespace StardewRoguelike
{
    /// <summary>A capability a keystone gains whole at one tier, rather than by degree.</summary>
    /// <remarks>
    /// Keystone levels were quantities and nothing else: the same effect with a bigger
    /// number behind it. That makes deepening a keystone a decision about arithmetic,
    /// and it makes the second and third levels of one read exactly like the first, so
    /// a shrine offering "Prospector II" is offering a percentage rather than a choice.
    ///
    /// A trait is the other kind of depth - the keystone starts doing something it did
    /// not do at all before - which is what makes a deep keystone a different keystone
    /// instead of more of the same one.
    /// </remarks>
    internal enum KeystoneTrait
    {
        /// <summary>Swinging a pickaxe costs no energy.</summary>
        FreeMining,

        /// <summary>A burst takes the rock around it, not only the rock that caused it.</summary>
        BreakAdjacent,

        /// <summary>A burst reaches a tile further than it did.</summary>
        WiderBurst,

        /// <summary>Boulders burst when they are broken, as rock does.</summary>
        BreakBoulders,

        /// <summary>A monster killed by a blast blows up in its turn, once.</summary>
        ChainKills,

        /// <summary>Whatever lives through a blast is left reeling.</summary>
        BlastStuns,

        /// <summary>A blast reaches a tile further.</summary>
        WiderBlast,

        /// <summary>A blast takes the rock it reaches, as well as the monsters.</summary>
        BlastBreaksRock,

        /// <summary>A blow that lands builds momentum, not only a blow that kills.</summary>
        StackOnHit,

        /// <summary>At full momentum, a kill gives a little health back.</summary>
        StackedSustain,

        /// <summary>Momentum stacks higher than it did.</summary>
        DeeperStacks,

        /// <summary>Momentum does not lapse while anything is still near enough to fight.</summary>
        HeldStacks,

        /// <summary>Damage armour turned away is banked as though it had landed.</summary>
        BankBlocked,

        /// <summary>What spite hands back splashes onto whatever stands beside the target.</summary>
        SpiteSplash,

        /// <summary>Spite holds twice as much as it did.</summary>
        BiggerBank,

        /// <summary>While nearly dead, less of what reaches the player gets through.</summary>
        Braced,

        /// <summary>While nearly dead, landing a blow gives a little health back.</summary>
        Leeching,

        /// <summary>Once a floor, a killing blow leaves the player on one health instead.</summary>
        SecondWind,

        /// <summary>A run of blows survives being hit once.</summary>
        StreakGrace,

        /// <summary>At a full run of blows, a swing reaches everything alongside the target.</summary>
        FlurrySweep,

        /// <summary>A run of blows counts higher than it did.</summary>
        LongerStreak,

        /// <summary>At a full run of blows, the weapon swings faster.</summary>
        FlurryHaste
    }

    /// <summary>Which keystone grants which trait, and how deep it has to be taken.</summary>
    /// <remarks>
    /// One table, read from three places that would otherwise drift apart: what the
    /// effects do, how far a keystone can be taken at all, and what a shrine says
    /// taking it would grant. A trait added here raises the perk's ceiling and turns
    /// up in its description without anything else being edited, so a trait cannot be
    /// written into the behaviour and then forgotten in the other two.
    /// </remarks>
    internal static class KeystoneTraits
    {
        private readonly record struct Grant(Perks.PerkType Perk, int Tier, KeystoneTrait Trait);

        private static readonly Grant[] Table =
        {
            new(Perks.PerkType.Prospector, 2, KeystoneTrait.FreeMining),
            new(Perks.PerkType.Prospector, 3, KeystoneTrait.BreakAdjacent),
            new(Perks.PerkType.Prospector, 4, KeystoneTrait.WiderBurst),
            new(Perks.PerkType.Prospector, 4, KeystoneTrait.BreakBoulders),

            new(Perks.PerkType.Volatile, 2, KeystoneTrait.ChainKills),
            new(Perks.PerkType.Volatile, 3, KeystoneTrait.BlastStuns),
            new(Perks.PerkType.Volatile, 4, KeystoneTrait.WiderBlast),
            new(Perks.PerkType.Volatile, 4, KeystoneTrait.BlastBreaksRock),

            new(Perks.PerkType.Momentum, 2, KeystoneTrait.StackOnHit),
            new(Perks.PerkType.Momentum, 3, KeystoneTrait.StackedSustain),
            new(Perks.PerkType.Momentum, 4, KeystoneTrait.DeeperStacks),
            new(Perks.PerkType.Momentum, 4, KeystoneTrait.HeldStacks),

            new(Perks.PerkType.Spite, 2, KeystoneTrait.BankBlocked),
            new(Perks.PerkType.Spite, 3, KeystoneTrait.SpiteSplash),
            new(Perks.PerkType.Spite, 4, KeystoneTrait.BiggerBank),

            new(Perks.PerkType.LastStand, 2, KeystoneTrait.Braced),
            new(Perks.PerkType.LastStand, 3, KeystoneTrait.Leeching),
            new(Perks.PerkType.LastStand, 4, KeystoneTrait.SecondWind),

            new(Perks.PerkType.Flurry, 2, KeystoneTrait.StreakGrace),
            new(Perks.PerkType.Flurry, 3, KeystoneTrait.FlurrySweep),
            new(Perks.PerkType.Flurry, 4, KeystoneTrait.LongerStreak),
            new(Perks.PerkType.Flurry, 4, KeystoneTrait.FlurryHaste)
        };

        /// <summary>The deepest tier this perk has a trait at, or zero if it has none.</summary>
        /// <remarks>
        /// What <see cref="Perks.MaxLevel"/> raises a keystone's ceiling to. A trait
        /// nobody can reach is not a trait, so the table decides how deep its own
        /// keystone goes rather than a number kept beside it.
        /// </remarks>
        public static int MaxTier(Perks.PerkType perk)
        {
            int deepest = 0;

            foreach (Grant grant in Table)
            {
                if (grant.Perk == perk && grant.Tier > deepest)
                    deepest = grant.Tier;
            }

            return deepest;
        }

        /// <summary>Whether the player has taken the granting perk far enough for this.</summary>
        public static bool Has(KeystoneTrait trait)
        {
            foreach (Grant grant in Table)
            {
                if (grant.Trait == trait && Perks.PerkLevel(grant.Perk) >= grant.Tier)
                    return true;
            }

            return false;
        }

        /// <summary>Whether <paramref name="tier"/> of this perk grants anything new.</summary>
        public static bool GrantsAnythingAt(Perks.PerkType perk, int tier)
        {
            foreach (Grant grant in Table)
            {
                if (grant.Perk == perk && grant.Tier == tier)
                    return true;
            }

            return false;
        }

        /// <summary>Every trait this perk has, shallowest tier first.</summary>
        public static IEnumerable<(int Tier, KeystoneTrait Trait)> For(Perks.PerkType perk)
        {
            foreach (Grant grant in Table)
            {
                if (grant.Perk == perk)
                    yield return (grant.Tier, grant.Trait);
            }
        }
    }
}

using StardewModdingAPI.Utilities;
using StardewValley;
using System;

namespace StardewRoguelike
{
    /// <summary>
    /// The one place health given for hitting and killing things is handed out.
    /// </summary>
    /// <remarks>
    /// Every heal of that kind in the run used to apply itself: the vampire ring two a
    /// kill, the Leech perk one a hit, Last Stand's leech two a hit, Momentum's three a
    /// kill, the vampiric enchantment up to thirty. Each was reasonable on its own and
    /// nothing added them up.
    ///
    /// The keystones then broke the assumption they were all written under, which was
    /// one hit and one kill per swing. A sweep hits five monsters, a splash hits five,
    /// a volatile cascade kills fifteen - so a seven hundred gold ring became thirty
    /// health a swing without the player investing in healing at all, and no single
    /// number anywhere was wrong.
    ///
    /// So the numbers stay and the rate is capped. What this buys beyond fixing the
    /// combinations that exist is that it fixes the ones that do not yet: a trait added
    /// later cannot multiply sustain past this without someone deciding to raise it.
    ///
    /// Only hit and kill effects come through here. A hot spring, a spa, food and a
    /// revive are not rate problems and are left to heal as much as they say they do.
    /// </remarks>
    internal static class Sustain
    {
        /// <summary>The most health that hitting and killing may give in one second.</summary>
        /// <remarks>
        /// Six, against a starting fifty. Enough that a build leaning on sustain feels
        /// it, and little enough that a crowd still out-damages it - which is the thing
        /// that had stopped being true.
        /// </remarks>
        private const int BudgetPerSecond = 6;

        /// <summary>Ticks in a second, after which the budget is handed back.</summary>
        private const int RefillTicks = 60;

        private static readonly PerScreen<int> ScreenRemaining = new(() => BudgetPerSecond);

        private static readonly PerScreen<int> ScreenTicks = new(() => 0);

        /// <summary>What this player's second has left to give.</summary>
        /// <remarks>
        /// Per screen, not static. Two local players share a budget only if they
        /// share a body: one of them fighting a crowd would otherwise spend the
        /// second the other was about to heal on, and the one who lost the race
        /// would see a vampire ring do nothing at all. The rest of the mod has kept
        /// run state per screen since perks did.
        /// </remarks>
        private static int Remaining
        {
            get => ScreenRemaining.Value;
            set => ScreenRemaining.Value = value;
        }

        /// <summary>Ticks since the budget was last handed back, for this player.</summary>
        private static int Ticks
        {
            get => ScreenTicks.Value;
            set => ScreenTicks.Value = value;
        }

        /// <summary>Hands the budget back once a second. Called from the update loop.</summary>
        public static void Update()
        {
            if (++Ticks < RefillTicks)
                return;

            Ticks = 0;
            Remaining = BudgetPerSecond;
        }

        /// <summary>
        /// Gives the player as much of <paramref name="amount"/> as the second allows.
        /// </summary>
        /// <returns>What was actually given, which may be none of it.</returns>
        public static int Heal(int amount)
        {
            if (amount <= 0 || Remaining <= 0)
                return 0;

            int allowed = Math.Min(amount, Remaining);
            Remaining -= allowed;

            Game1.player.health = Math.Min(Game1.player.health + allowed, Game1.player.maxHealth);

            return allowed;
        }

        /// <summary>Forgets the second in progress. Called when a run begins.</summary>
        public static void Reset()
        {
            Remaining = BudgetPerSecond;
            Ticks = 0;
        }
    }
}

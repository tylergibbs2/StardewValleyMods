using StardewRoguelike.ChallengeFloors;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StardewRoguelike.Run
{
    /// <summary>
    /// What the run offers, worked out from the seed alone.
    ///
    /// Floor types used to be decided one floor at a time inside GetMine, each
    /// predicate rolling FloorRng and reading whichever mines were loaded at that
    /// moment - so what a floor became depended on the order floors were visited and
    /// on which old levels had been discarded. The plan is built from level one
    /// upward every time, so it answers the same way wherever the player is standing,
    /// and every machine derives the same answer from the same seed without anything
    /// being sent over the network.
    ///
    /// The plan only says what is offered. What a floor actually becomes is the
    /// group's choice, which <see cref="RunRoute"/> holds.
    /// </summary>
    internal static class RunPlan
    {
        /// <summary>How many exits a floor offers, when it offers a choice at all.</summary>
        public const int OfferCount = 3;

        /// <summary>
        /// Forges are a power spike, so they are kept apart.
        ///
        /// Here it only keeps them out of the opening floors, where there is no weapon
        /// worth enchanting yet. The spacing between one forge and the next is
        /// <see cref="RunRoute.ForgeInterval"/>, which counts the forges the group
        /// actually took rather than the ones it was shown.
        /// </summary>
        public const int ForgeMinimumLevel = 10;

        /// <summary>
        /// A dark mineshaft is not offered before this level.
        ///
        /// The opening floors are where a player is working out what the exits mean,
        /// carrying the weapon they started with, which is not the moment to put the
        /// lights out.
        /// </summary>
        private const int DarkMinimumLevel = 5;

        /// <summary>
        /// A cauldron is not offered before this level.
        ///
        /// Nothing to cleanse before the first boss: curses come from shrines, and a
        /// floor whose whole purpose is undoing one is a wasted exit until the player
        /// has had the chance to take one.
        /// </summary>
        private const int CauldronMinimumLevel = 5;

        /// <summary>
        /// The treasure floors a Vault exit can lead to.
        ///
        /// Three of these had no way of being reached at all: the plan only ever
        /// asked for a combat floor, and the dwarf's chests, the gambling chests and
        /// the two doors are none of them combat. They are loot, so they belong
        /// behind the exit that promises loot, in the same way the eight combat
        /// floors sit behind the one that promises a fight.
        /// </summary>
        private static readonly ChallengeFloor.ChallengeType?[] VaultFloors =
        {
            null,                                      // the plain chest floor
            ChallengeFloor.ChallengeType.DwarfChests,
            ChallengeFloor.ChallengeType.GambaChests,
            ChallengeFloor.ChallengeType.PickAPath
        };

        /// <summary>The combat floors an Arena exit can lead to.</summary>
        private static readonly ChallengeFloor.ChallengeType[] ArenaChallenges =
        {
            ChallengeFloor.ChallengeType.KOTH,
            ChallengeFloor.ChallengeType.TimedKills,
            ChallengeFloor.ChallengeType.Race,
            ChallengeFloor.ChallengeType.SlingshotAim,
            ChallengeFloor.ChallengeType.JOTPK,
            ChallengeFloor.ChallengeType.HiddenBoss,
            ChallengeFloor.ChallengeType.EggHunt,
            ChallengeFloor.ChallengeType.Speedrun
        };

        /// <summary>
        /// Draw weights for a choice. Normal and Arena are the backbone; Vault and
        /// Spring are situational; Forge is rare on top of its spacing rule.
        /// </summary>
        private static readonly (FloorKind Kind, int Weight)[] Pool =
        {
            (FloorKind.Normal, 3),
            (FloorKind.DarkMineshaft, 2),
            (FloorKind.Cauldron, 2),
            (FloorKind.Arena, 3),
            (FloorKind.Vault, 2),
            (FloorKind.Spring, 2),
            (FloorKind.Shrine, 2),
            (FloorKind.Forge, 1)
        };

        private sealed class Entry
        {
            public FloorKind[] Offers = Array.Empty<FloorKind>();
        }

        /// <summary>The seed the cache below was built for; -1 when nothing is built.</summary>
        private static int builtForSeed = -1;

        /// <summary>Entries by level. Index 0 is unused; the run starts at level 1.</summary>
        private static readonly List<Entry> entries = new() { new Entry() };

        /// <summary>Throws the cache away so the next read rebuilds from the seed.</summary>
        public static void Invalidate()
        {
            builtForSeed = -1;
            arenaOrderSeed = -1;
            shrineOrderSeed = -1;
            vaultOrderSeed = -1;
        }

        /// <summary>
        /// The order a shrine on <paramref name="level"/> will offer perks in.
        ///
        /// Every perk, once, most likely first. A shrine takes the first two of these
        /// the player can still take, which keeps the offer fixed before anybody
        /// reaches it: the exit menu names what is waiting, and naming it means it
        /// cannot be rolled when the menu opens.
        ///
        /// Weighted the way the old roll was - a common perk is about twice as likely
        /// to come up early as a rare one - but as an order rather than a draw, so
        /// reading it twice gives the same answer.
        /// </summary>
        public static Perks.PerkType[] ShrineOrderFor(int level)
        {
            int seed = Roguelike.FloorRngSeed;

            if (shrineOrderSeed != seed)
            {
                shrineOrders.Clear();
                shrineOrderSeed = seed;
            }

            if (shrineOrders.TryGetValue(level, out Perks.PerkType[]? order))
                return order;

            order = BuildShrineOrder(seed, level);
            shrineOrders[level] = order;

            return order;
        }

        /// <summary>
        /// Which treasure floor the run's <paramref name="index"/>th Vault leads to.
        ///
        /// Dealt from a bag and counted in vaults taken, for the same reasons the
        /// arenas are: rolling each one would have a player meet two or three of the
        /// four in a run, and a bag that advanced per floor rather than per vault
        /// would be a roll again in all but name.
        /// </summary>
        public static ChallengeFloor.ChallengeType? VaultAt(int index)
        {
            if (index < 0)
                index = 0;

            BuildVaultOrder(index);

            return vaultOrder[index];
        }

        private static readonly List<ChallengeFloor.ChallengeType?> vaultOrder = new();

        private static int vaultOrderSeed = -1;

        /// <summary>Keeps the treasure order off every other stream.</summary>
        private const int VaultStream = 0x7A17;

        private static void BuildVaultOrder(int upTo)
        {
            int seed = Roguelike.FloorRngSeed;

            if (vaultOrderSeed == seed && upTo < vaultOrder.Count)
                return;

            vaultOrder.Clear();
            vaultOrderSeed = seed;

            Random rng = new(seed ^ VaultStream);

            while (vaultOrder.Count <= upTo)
            {
                List<ChallengeFloor.ChallengeType?> bag = new(VaultFloors);

                do
                {
                    for (int i = bag.Count - 1; i > 0; i--)
                    {
                        int j = rng.Next(i + 1);
                        (bag[i], bag[j]) = (bag[j], bag[i]);
                    }
                }
                while (vaultOrder.Count > 0 && bag[0] == vaultOrder[^1]);

                vaultOrder.AddRange(bag);
            }
        }

        /// <summary>The orders built so far, by level.</summary>
        private static readonly Dictionary<int, Perks.PerkType[]> shrineOrders = new();

        /// <summary>The seed those were built for; -1 when nothing is built.</summary>
        private static int shrineOrderSeed = -1;

        /// <summary>Keeps the perk order off the streams the floors are drawn from.</summary>
        private const int ShrineStream = 0x5A41;

        /// <summary>
        /// How much likelier a perk of each rarity is to come up early.
        ///
        /// The old roll picked a rarity at sixty, thirty and ten per cent and then
        /// picked evenly inside it, which works out at about five to three to two per
        /// perk once the pools are different sizes. These are those odds, as weights.
        /// </summary>
        private static int WeightOf(Perks.PerkType perk)
        {
            return Perks.GetPerkRarity(perk) switch
            {
                Perks.PerkRarity.Common => 5,
                Perks.PerkRarity.Uncommon => 3,

                // A keystone is already guaranteed at the first shrine, so in the
                // ordinary draw it is the rarest thing there is.
                Perks.PerkRarity.Keystone => 1,
                _ => 2
            };
        }

        private static Perks.PerkType[] BuildShrineOrder(int seed, int level)
        {
            // A stream of its own per level, so the order at one shrine says nothing
            // about the order at the next and extending the floor plan cannot move it.
            Random rng = new(seed ^ ShrineStream ^ (level * 7919));

            List<Perks.PerkType> pool = new(Enum.GetValues<Perks.PerkType>());
            List<Perks.PerkType> order = new(pool.Count);

            while (pool.Count > 0)
            {
                int total = pool.Sum(WeightOf);
                int roll = rng.Next(total);
                int index = 0;

                for (; index < pool.Count - 1; index++)
                {
                    roll -= WeightOf(pool[index]);
                    if (roll < 0)
                        break;
                }

                order.Add(pool[index]);
                pool.RemoveAt(index);
            }

            return order.ToArray();
        }

        /// <summary>
        /// Which combat floor the run's <paramref name="index"/>th Arena leads to,
        /// counting from zero.
        ///
        /// Drawn from a bag rather than rolled, so all eight come round before any of
        /// them comes round twice. Rolling each one independently meant a run that
        /// took eight arenas saw about five of the eight kinds, with repeats, and
        /// stood a one in three chance of never meeting a given one at all.
        ///
        /// Counted in arenas rather than in floors on purpose. A bag that advanced
        /// once per level would be sampled eight times out of thirty by a player who
        /// takes some other exit most of the time, which is a roll again in all but
        /// name.
        /// </summary>
        public static ChallengeFloor.ChallengeType ArenaAt(int index)
        {
            if (index < 0)
                index = 0;

            BuildArenaOrder(index);

            return arenaOrder[index];
        }

        /// <summary>The order the combat floors come in; see <see cref="ArenaAt"/>.</summary>
        private static readonly List<ChallengeFloor.ChallengeType> arenaOrder = new();

        /// <summary>The seed the order above was built for; -1 when nothing is built.</summary>
        private static int arenaOrderSeed = -1;

        /// <summary>
        /// Keeps the arena order on a different stream from the offers, so that
        /// changing how one is drawn does not shuffle the other.
        /// </summary>
        private const int ArenaStream = 0x5A1D;

        private static void BuildArenaOrder(int upTo)
        {
            int seed = Roguelike.FloorRngSeed;

            if (arenaOrderSeed == seed && upTo < arenaOrder.Count)
                return;

            // Rebuilt from the start rather than extended, so the order already dealt
            // out cannot change when more of it is needed.
            arenaOrder.Clear();
            arenaOrderSeed = seed;

            Random rng = new(seed ^ ArenaStream);

            while (arenaOrder.Count <= upTo)
            {
                List<ChallengeFloor.ChallengeType> bag = new(ArenaChallenges);

                // A bag that opens with what the last one closed on reads as a repeat
                // however fairly it was dealt, so it is shuffled again.
                do
                {
                    Shuffle(bag, rng);
                }
                while (arenaOrder.Count > 0 && bag[0] == arenaOrder[^1]);

                arenaOrder.AddRange(bag);
            }
        }

        private static void Shuffle(List<ChallengeFloor.ChallengeType> bag, Random rng)
        {
            for (int i = bag.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (bag[i], bag[j]) = (bag[j], bag[i]);
            }
        }

        /// <summary>
        /// Every kind the seed will consider for <paramref name="level"/>, best first.
        ///
        /// The whole pool rather than the three that get shown, so that
        /// <see cref="RunRoute.OffersFor"/> has spares to promote when it drops a kind
        /// the group has already taken this boss cycle. Without them a late floor in a
        /// cycle would be left offering one exit, which is not a choice.
        /// </summary>
        public static FloorKind[] OffersFor(int level) => Get(level).Offers;

        /// <summary>Whether <paramref name="level"/> is forced and offers no choice.</summary>
        public static bool IsForced(int level) =>
            Merchant.IsMerchantFloor(level) || BossFloor.IsBossFloor(level);

        private static Entry Get(int level)
        {
            if (level < 1)
                level = 1;

            Build(level);
            return entries[level];
        }

        /// <summary>
        /// Fills the cache up to <paramref name="upTo"/>, rebuilding from level one if
        /// the seed has changed since it was last built.
        ///
        /// Always from level one, never from wherever the player happens to be. A
        /// level is planned from one draw after another off a single stream, so where
        /// that stream has got to is what decides the answer, and starting it part way
        /// through would give a different one on a machine that had visited fewer
        /// floors.
        /// </summary>
        private static void Build(int upTo)
        {
            int seed = Roguelike.FloorRngSeed;

            if (builtForSeed != seed)
            {
                entries.Clear();
                entries.Add(new Entry());
                builtForSeed = seed;
            }

            if (upTo < entries.Count)
                return;

            // A stream of its own rather than FloorRng: the plan must not depend on
            // how much of the run's randomness has been spent elsewhere. Replayed
            // from the start so that extending the cache cannot change the levels
            // already in it.
            Random rng = new(seed);
            for (int level = 1; level < entries.Count; level++)
                Plan(new Entry(), rng, level);

            for (int level = entries.Count; level <= upTo; level++)
            {
                Entry entry = new();
                entries.Add(entry);
                Plan(entry, rng, level);
            }
        }

        private static void Plan(Entry entry, Random rng, int level)
        {
            if (Merchant.IsMerchantFloor(level))
            {
                entry.Offers = new[] { FloorKind.Merchant };
                return;
            }

            if (BossFloor.IsBossFloor(level))
            {
                entry.Offers = new[] { FloorKind.Boss };
                return;
            }

            List<(FloorKind Kind, int Weight)> pool = Pool
                .Where(candidate => Allowed(candidate.Kind, level))
                .ToList();

            List<FloorKind> offers = new();
            while (pool.Count > 0)
            {
                FloorKind picked = Draw(pool, rng);
                offers.Add(picked);
                pool.RemoveAll(candidate => candidate.Kind == picked);
            }

            // There has to be a floor worth playing rather than only a heal and a
            // chest, or a hurt player with no gold is left with nothing to pick. Only
            // the head of the list is shown, so that is where one has to be.
            Promote(offers);

            entry.Offers = offers.ToArray();
        }

        /// <summary>
        /// Moves a mining or combat floor into the shown part of the list, if the draw
        /// did not put one there.
        /// </summary>
        private static void Promote(List<FloorKind> offers)
        {
            for (int i = 0; i < Math.Min(OfferCount, offers.Count); i++)
            {
                if (offers[i] == FloorKind.Normal || offers[i] == FloorKind.Arena)
                    return;
            }

            int found = offers.FindIndex(kind => kind == FloorKind.Normal || kind == FloorKind.Arena);
            if (found < 0)
                return;

            int last = Math.Min(OfferCount, offers.Count) - 1;
            (offers[last], offers[found]) = (offers[found], offers[last]);
        }

        /// <remarks>
        /// Spacing used to live here, as a look back over what earlier levels had been
        /// offered. It no longer does: how often a kind comes round is decided by what
        /// the group took, in <see cref="RunRoute.OffersFor"/>, and a plan that also
        /// held kinds back left some floors with one exit on them. A level is now
        /// planned without reference to any other, which is what the cache was
        /// rebuilding from level one to guarantee.
        /// </remarks>
        private static bool Allowed(FloorKind kind, int level) => kind switch
        {
            FloorKind.Forge => level > ForgeMinimumLevel,
            FloorKind.DarkMineshaft => level > DarkMinimumLevel,
            FloorKind.Cauldron => level > CauldronMinimumLevel,
            _ => true
        };

        private static FloorKind Draw(List<(FloorKind Kind, int Weight)> pool, Random rng)
        {
            int total = pool.Sum(candidate => candidate.Weight);
            int roll = rng.Next(total);

            foreach ((FloorKind kind, int weight) in pool)
            {
                roll -= weight;
                if (roll < 0)
                    return kind;
            }

            return pool[^1].Kind;
        }
    }
}

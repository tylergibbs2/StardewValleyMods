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

        /// <summary>Forges are a power spike, so they are kept apart.</summary>
        private const int ForgeMinimumInterval = 10;

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
            (FloorKind.Arena, 3),
            (FloorKind.Vault, 2),
            (FloorKind.Spring, 2),
            (FloorKind.Forge, 1)
        };

        private sealed class Entry
        {
            public FloorKind[] Offers = Array.Empty<FloorKind>();
            public ChallengeFloor.ChallengeType Arena;
        }

        /// <summary>The seed the cache below was built for; -1 when nothing is built.</summary>
        private static int builtForSeed = -1;

        /// <summary>Entries by level. Index 0 is unused; the run starts at level 1.</summary>
        private static readonly List<Entry> entries = new() { new Entry() };

        /// <summary>Throws the cache away so the next read rebuilds from the seed.</summary>
        public static void Invalidate() => builtForSeed = -1;

        /// <summary>The kinds offered as exits leading to <paramref name="level"/>.</summary>
        public static FloorKind[] OffersFor(int level) => Get(level).Offers;

        /// <summary>Which combat floor an Arena at <paramref name="level"/> leads to.</summary>
        public static ChallengeFloor.ChallengeType ArenaFor(int level) => Get(level).Arena;

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
        /// Always from level one, never from wherever the player happens to be: the
        /// spacing rules look back at earlier levels, so a partial build would give
        /// different answers depending on where it started.
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
            while (offers.Count < OfferCount && pool.Count > 0)
            {
                FloorKind picked = Draw(pool, rng);
                offers.Add(picked);
                pool.RemoveAll(candidate => candidate.Kind == picked);
            }

            // There has to be a floor worth playing rather than only a heal and a
            // chest, or a hurt player with no gold is left with nothing to pick.
            if (!offers.Contains(FloorKind.Normal) && !offers.Contains(FloorKind.Arena))
                offers[^1] = FloorKind.Normal;

            entry.Offers = offers.ToArray();
            entry.Arena = ArenaChallenges[rng.Next(ArenaChallenges.Length)];
        }

        private static bool Allowed(FloorKind kind, int level) => kind switch
        {
            // Spaced out, and never in the opening floors where there is no weapon
            // worth enchanting yet.
            FloorKind.Forge => level > ForgeMinimumInterval
                               && NoRecentOffer(FloorKind.Forge, level, ForgeMinimumInterval),

            // Back-to-back springs waste a choice; one is enough.
            FloorKind.Spring => NoRecentOffer(FloorKind.Spring, level, 1),

            _ => true
        };

        private static bool NoRecentOffer(FloorKind kind, int level, int within)
        {
            for (int back = 1; back <= within; back++)
            {
                int earlier = level - back;
                if (earlier < 1 || earlier >= entries.Count)
                    continue;

                if (entries[earlier].Offers.Contains(kind))
                    return false;
            }

            return true;
        }

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

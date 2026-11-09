using Netcode;
using StardewModdingAPI;
using StardewRoguelike.VirtualProperties;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Network;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StardewRoguelike.Run
{
    /// <summary>
    /// The route the group has actually taken: which of the offered kinds was chosen
    /// for each level.
    ///
    /// One route per run, not one per player. The first player to commit at an exit
    /// settles that level for everyone, which matches how the merchant floors already
    /// hold the group together rather than letting people wander off separately.
    /// </summary>
    internal static class RunRoute
    {
        /// <summary>The message farmhands send to have the host record their choice.</summary>
        public const string MessageType = "RouteChoice";

        /// <summary>
        /// The floors that ask nothing of the player: walk in, take the reward, leave.
        ///
        /// One of these per boss cycle. Two makes half the run between bosses a
        /// collection round, and a cycle that costs nothing is a cycle with no reason
        /// to weigh one exit against another. The forge is not among them, although it
        /// is just as quiet: it has its own interval, and counting it here left a fifth
        /// of all exits with a single option, since a cycle is only four floors.
        /// </summary>
        private static readonly FloorKind[] Friendly =
        {
            FloorKind.Spring,
            FloorKind.Vault
        };

        /// <summary>How many floors of a kind one cycle may hold; one unless named.</summary>
        /// <remarks>
        /// The rule that a kind spends itself reads a second visit as the same
        /// decision made twice, which is true of every kind whose floor is built the
        /// same way each time. An arena is the one that is not: the combat floor it
        /// leads to comes off a bag that advances per arena played, so a second arena
        /// in a cycle is a different challenge rather than the first one again.
        ///
        /// Two, and not simply as many as the player likes. Lifting the limit
        /// altogether read well for anybody choosing an arena now and then and badly
        /// for anybody choosing every one offered: that route filled half the run with
        /// combat and ended it on one shrine, so the keystone it was fighting to use
        /// never arrived. A second arena costs a shrine, which is a trade worth
        /// weighing; a fifth costs the build, which is not a trade but a trap. Two
        /// keeps very nearly all of what lifting the limit was worth to an ordinary
        /// route and none of what it did to that one.
        /// </remarks>
        private static readonly Dictionary<FloorKind, int> PerCycleLimit = new()
        {
            [FloorKind.Arena] = 2
        };

        /// <summary>How many levels have to pass between one forge and the next.</summary>
        /// <remarks>
        /// Counted from the forges the group took, not the ones it was offered, since
        /// it is taking one that spikes the run's power. A forge is also at most one
        /// per boss cycle by the rule every kind is under, so this is what stops two
        /// cycles in a row ending in an enchantment.
        /// </remarks>
        private const int ForgeInterval = 10;

        public class ChoicePayload
        {
            public int Level { get; set; }
            public int Kind { get; set; }
        }

        private static NetIntDictionary<int, NetInt> Store =>
            Game1.player.team.get_FarmerTeamRoute();

        /// <summary>What <paramref name="level"/> is, once choices are taken into account.</summary>
        public static FloorKind KindFor(int level)
        {
            if (Merchant.IsMerchantFloor(level))
                return FloorKind.Merchant;

            if (BossFloor.IsBossFloor(level))
                return FloorKind.Boss;

            // Named outright, so it wins over the older per-kind flags below.
            if (DebugCommands.ForcedFloorKindIndex != -1)
            {
                FloorKind[] kinds = Enum.GetValues<FloorKind>();

                return kinds[Math.Clamp(DebugCommands.ForcedFloorKindIndex, 0, kinds.Length - 1)];
            }

            // The rdebug force flags used to be read by each ShouldDoXFloor predicate.
            // They are overrides on the resolved kind now, so one place answers what a
            // floor is and the debug commands keep working.
            if (DebugCommands.ForcedChallengeIndex != -1)
                return FloorKind.Arena;
            if (DebugCommands.ForcedChestFloor)
                return FloorKind.Vault;
            if (DebugCommands.ForcedForgeFloor)
                return FloorKind.Forge;

            if (Store.TryGetValue(level, out int chosen))
                return (FloorKind)chosen;

            // Nobody committed - a debug warp, or a floor reached some way that skips
            // the exit menu. Fall back to the plan's first offer so the floor still
            // generates the same way on every machine.
            FloorKind[] offers = OffersFor(level);
            return offers.Length > 0 ? offers[0] : FloorKind.Normal;
        }

        /// <summary>The kinds actually offered at the exit leading to <paramref name="level"/>.</summary>
        /// <remarks>
        /// What the plan would offer, less whatever the group has already taken since
        /// the last boss. A cycle that allows three springs is a cycle with one
        /// decision in it repeated; dropping what has been used makes each choice
        /// spend itself, so a cycle reads as a route rather than four draws from the
        /// same bag, up to the allowance in <see cref="PerCycleLimit"/>.
        ///
        /// The plan hands over its whole pool, so dropping a kind promotes the next
        /// one rather than shortening the menu. If even that is exhausted - a long
        /// cycle, or a level where the spacing rules allow little - the plan's own
        /// head is used, because an exit with nothing on it is a dead end.
        /// </remarks>
        public static FloorKind[] OffersFor(int level)
        {
            FloorKind[] planned = RunPlan.OffersFor(level);
            if (planned.Length <= 1)
                return planned;

            Dictionary<FloorKind, int> taken = TakenThisCycle(level);

            HashSet<FloorKind> blocked = taken
                .Where(entry => entry.Value >= LimitFor(entry.Key))
                .Select(entry => entry.Key)
                .ToHashSet();

            // One quiet floor per cycle, so taking either of them spends both.
            if (Friendly.Any(blocked.Contains))
                foreach (FloorKind kind in Friendly)
                    blocked.Add(kind);

            if (RecentlyForged(level))
                blocked.Add(FloorKind.Forge);

            FloorKind[] fresh = planned
                .Where(kind => !blocked.Contains(kind))
                .Take(RunPlan.OfferCount)
                .ToArray();

            return fresh.Length > 0 ? fresh : planned.Take(RunPlan.OfferCount).ToArray();
        }

        /// <summary>Whether a forge was taken too recently to offer another.</summary>
        private static bool RecentlyForged(int level)
        {
            for (int back = 1; back <= ForgeInterval; back++)
            {
                int earlier = level - back;
                if (earlier < 1)
                    break;

                if (Store.TryGetValue(earlier, out int chosen) && (FloorKind)chosen == FloorKind.Forge)
                    return true;
            }

            return false;
        }

        /// <summary>How many of each kind the group has chosen since the last boss floor.</summary>
        /// <remarks>
        /// Read back off the route rather than counted forward, so it gives the same
        /// answer on a farmhand that joined mid-run as on the host.
        /// </remarks>
        private static Dictionary<FloorKind, int> TakenThisCycle(int level)
        {
            Dictionary<FloorKind, int> taken = new();

            for (int earlier = level - 1; earlier >= 1; earlier--)
            {
                if (BossFloor.IsBossFloor(earlier))
                    break;

                if (Store.TryGetValue(earlier, out int chosen))
                {
                    FloorKind kind = (FloorKind)chosen;
                    taken[kind] = taken.GetValueOrDefault(kind) + 1;
                }
            }

            return taken;
        }

        /// <summary>How many of <paramref name="kind"/> one cycle may hold.</summary>
        private static int LimitFor(FloorKind kind)
        {
            return PerCycleLimit.GetValueOrDefault(kind, 1);
        }

        /// <summary>Which combat floor an Arena at <paramref name="level"/> leads to.</summary>
        /// <remarks>
        /// The debug override is read here, beside the one in <see cref="KindFor"/>,
        /// rather than in the plan - which answers from the seed alone and should not
        /// know about debug commands.
        ///
        /// Both halves are needed. KindFor turns a forced index into an Arena; without
        /// this, rdebug forcechallenge gave an endless run of arenas of whatever kind
        /// the seed happened to name, which is not what it was asked for.
        /// </remarks>
        public static ChallengeFloor.ChallengeType ArenaFor(int level)
        {
            if (DebugCommands.ForcedChallengeIndex == -1)
                return RunPlan.ArenaAt(ArenasBefore(level));

            ChallengeFloor.ChallengeType[] all = Enum.GetValues<ChallengeFloor.ChallengeType>();
            int index = Math.Clamp(DebugCommands.ForcedChallengeIndex, 0, all.Length - 1);

            return all[index];
        }

        /// <summary>Which treasure floor a Vault at <paramref name="level"/> leads to.</summary>
        public static ChallengeFloor.ChallengeType? VaultFor(int level)
        {
            if (DebugCommands.ForcedChestFloor)
                return null;

            return RunPlan.VaultAt(TakenBefore(level, FloorKind.Vault));
        }

        /// <summary>How many Arenas the group took before reaching this one.</summary>
        /// <remarks>
        /// Read back off the route, so it is the same number on every machine and on
        /// a farmhand that joined mid-run. It is this that makes the bag in
        /// <see cref="RunPlan.ArenaAt"/> deal one combat floor per Arena played
        /// rather than one per floor walked past.
        /// </remarks>
        private static int ArenasBefore(int level)
        {
            return TakenBefore(level, FloorKind.Arena);
        }

        /// <summary>How many floors of one kind the group took before this level.</summary>
        /// <remarks>
        /// Read back off the route, so it is the same number on every machine and on
        /// a farmhand that joined mid-run. It is this that makes the bags deal one
        /// floor per floor of that kind played, rather than one per floor walked past.
        /// </remarks>
        private static int TakenBefore(int level, FloorKind kind)
        {
            int count = 0;

            for (int earlier = 1; earlier < level; earlier++)
            {
                if (Store.TryGetValue(earlier, out int chosen) && (FloorKind)chosen == kind)
                    count++;
            }

            return count;
        }

        /// <summary>Whether <paramref name="mine"/> is a floor the shrine stands on.</summary>
        /// <remarks>
        /// Asked from three places that each have to leave a shrine floor alone, so it
        /// is answered once here rather than spelled out at each of them.
        /// </remarks>
        public static bool IsShrineFloor(MineShaft mine) =>
            KindFor(Roguelike.GetLevelFromMineshaft(mine)) == FloorKind.Shrine;

        /// <summary>Whether the group has already settled <paramref name="level"/>.</summary>
        public static bool IsSettled(int level) =>
            RunPlan.IsForced(level) || Store.ContainsKey(level);

        /// <summary>
        /// Whether somebody actually chose <paramref name="level"/> at an exit.
        /// </summary>
        /// <remarks>
        /// Narrower than <see cref="IsSettled"/>, which also counts the floors
        /// nobody chooses because they are forced - every merchant and every boss,
        /// at any depth. That is the right answer for the exit, which only has to
        /// know whether to offer a menu, and the wrong one for anything asking what
        /// the group knows about the road: a boss forty floors down is settled and
        /// has never been seen.
        /// </remarks>
        public static bool WasChosen(int level) => Store.ContainsKey(level);

        /// <summary>
        /// Commits <paramref name="kind"/> for <paramref name="level"/>.
        ///
        /// Routed through the host rather than written locally: the route is a team
        /// net field, and having one writer keeps two players picking at the same
        /// moment from settling the same level differently on different machines.
        /// </summary>
        public static void Choose(int level, FloorKind kind)
        {
            if (RunPlan.IsForced(level) || Store.ContainsKey(level))
                return;

            if (Context.IsMainPlayer)
            {
                Store[level] = (int)kind;
                return;
            }

            ModEntry.MultiplayerHelper.SendMessage(
                new ChoicePayload { Level = level, Kind = (int)kind },
                MessageType,
                modIDs: new[] { ModEntry.ModId });
        }

        /// <summary>Applies a farmhand's choice. Host only.</summary>
        public static void Receive(ChoicePayload payload)
        {
            if (!Context.IsMainPlayer)
                return;

            if (RunPlan.IsForced(payload.Level) || Store.ContainsKey(payload.Level))
                return;

            Store[payload.Level] = payload.Kind;
        }

        /// <summary>Forgets the route. Called when a run begins.</summary>
        public static void Clear()
        {
            RunPlan.Invalidate();

            if (Context.IsMainPlayer)
                Store.Clear();
        }
    }
}

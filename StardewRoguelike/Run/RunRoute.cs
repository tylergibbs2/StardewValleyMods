using Netcode;
using StardewModdingAPI;
using StardewRoguelike.VirtualProperties;
using StardewValley;
using StardewValley.Network;
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
        /// same bag.
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

            HashSet<FloorKind> blocked = TakenThisCycle(level);

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

        /// <summary>The kinds the group has chosen since the last boss floor.</summary>
        /// <remarks>
        /// Read back off the route rather than counted forward, so it gives the same
        /// answer on a farmhand that joined mid-run as on the host.
        /// </remarks>
        private static HashSet<FloorKind> TakenThisCycle(int level)
        {
            HashSet<FloorKind> taken = new();

            for (int earlier = level - 1; earlier >= 1; earlier--)
            {
                if (BossFloor.IsBossFloor(earlier))
                    break;

                if (Store.TryGetValue(earlier, out int chosen))
                    taken.Add((FloorKind)chosen);
            }

            return taken;
        }

        /// <summary>Whether the group has already settled <paramref name="level"/>.</summary>
        public static bool IsSettled(int level) =>
            RunPlan.IsForced(level) || Store.ContainsKey(level);

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

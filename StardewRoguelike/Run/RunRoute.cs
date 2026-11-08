using Netcode;
using StardewModdingAPI;
using StardewRoguelike.VirtualProperties;
using StardewValley;
using StardewValley.Network;

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
            FloorKind[] offers = RunPlan.OffersFor(level);
            return offers.Length > 0 ? offers[0] : FloorKind.Normal;
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

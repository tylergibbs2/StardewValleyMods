using Netcode;
using StardewValley;
using StardewValley.Network;
using System.Runtime.CompilerServices;

namespace StardewRoguelike.VirtualProperties
{
    /// <summary>
    /// The run's floor-generation seed, held on the team so it is one value for the
    /// whole save and reaches farmhands. Only the host can set it: the seed terminal
    /// refuses anyone else.
    /// </summary>
    public static class FarmerTeamFloorRngSeed
    {
        internal class Holder { public readonly NetInt Value = new(); }

        internal static ConditionalWeakTable<FarmerTeam, Holder> values = new();

        public static NetInt get_FarmerTeamFloorRngSeed(this FarmerTeam team)
        {
            var holder = values.GetOrCreateValue(team);
            return holder.Value;
        }
    }

    /// <summary>
    /// Which floor kind the group chose for each level, keyed by level.
    ///
    /// On the team because the route belongs to the run rather than to a player, and
    /// net-synced because it is the one part of the run plan that is not derivable
    /// from the seed - it is a decision, not a roll.
    /// </summary>
    public static class FarmerTeamRoute
    {
        internal class Holder { public readonly NetIntDictionary<int, NetInt> Value = new(); }

        internal static ConditionalWeakTable<FarmerTeam, Holder> values = new();

        public static NetIntDictionary<int, NetInt> get_FarmerTeamRoute(this FarmerTeam team)
        {
            var holder = values.GetOrCreateValue(team);
            return holder.Value;
        }
    }

    public static class FarmerTeamHardMode
    {
        internal class Holder { public readonly NetBool Value = new(); }

        internal static ConditionalWeakTable<FarmerTeam, Holder> values = new();

        public static NetBool get_FarmerTeamHardMode(this FarmerTeam team)
        {
            var holder = values.GetOrCreateValue(team);
            return holder.Value;
        }
    }
}

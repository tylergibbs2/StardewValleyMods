using Netcode;
using StardewValley;
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

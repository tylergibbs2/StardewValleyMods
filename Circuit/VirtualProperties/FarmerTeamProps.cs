using Netcode;
using StardewValley;
using System.Runtime.CompilerServices;

namespace Circuit.VirtualProperties
{
    public static class FarmerTeamCurrentEvent
    {
        internal class Holder { public readonly NetRef<EventBase> Value = new(); }

        internal static ConditionalWeakTable<FarmerTeam, Holder> Values = new();

        public static NetRef<EventBase> get_FarmerTeamCurrentEvent(this FarmerTeam team)
        {
            Holder holder = Values.GetOrCreateValue(team);
            return holder.Value;
        }
    }
}

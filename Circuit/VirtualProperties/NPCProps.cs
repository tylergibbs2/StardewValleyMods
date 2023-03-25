using System.Runtime.CompilerServices;
using StardewValley;

namespace Circuit.VirtualProperties
{
    public static class NPCIsSwapped
    {
        internal class Holder { public bool Value = false; }

        internal static ConditionalWeakTable<NPC, Holder> Values = new();

        public static void set_NPCIsSwapped(this NPC npc, bool newValue)
        {
            Holder holder = Values.GetOrCreateValue(npc);
            holder.Value = newValue;
        }

        public static bool get_NPCIsSwapped(this NPC npc)
        {
            Holder holder = Values.GetOrCreateValue(npc);
            return holder.Value;
        }
    }
}

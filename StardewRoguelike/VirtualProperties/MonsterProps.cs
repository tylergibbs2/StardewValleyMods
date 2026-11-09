using StardewValley.Monsters;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace StardewRoguelike.VirtualProperties
{
    /// <summary>
    /// When each player's blows may next land on a monster, one deadline per player.
    /// </summary>
    /// <remarks>
    /// <see cref="Monster.invincibleCountdown"/> is a plain int, marked XmlIgnore and
    /// absent from Monster's net fields, so it is local to whichever machine wrote it.
    /// Over the network that works out in the players' favour by accident: each
    /// farmhand deserialises its own copy of the monster and so gets its own window,
    /// and a party's damage adds up.
    ///
    /// Split-screen players share one machine and therefore one Monster object, so
    /// they shared the field and the window with it - two players at one boss did
    /// roughly the damage of one, while the same two players online did double. This
    /// keeps a deadline per player instead, so the window means the same thing however
    /// the party happens to be sitting.
    /// </remarks>
    public static class MonsterInvincibleUntil
    {
        internal class Holder { public readonly Dictionary<long, double> Value = new(); }

        internal static readonly ConditionalWeakTable<Monster, Holder> values = new();

        /// <summary>This monster's deadlines, keyed by <c>UniqueMultiplayerID</c>.</summary>
        public static Dictionary<long, double> get_MonsterInvincibleUntil(this Monster monster)
        {
            return values.GetOrCreateValue(monster).Value;
        }
    }
}

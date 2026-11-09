using HarmonyLib;
using StardewValley;
using StardewValley.Objects;

namespace StardewRoguelike.Patches
{
    /// <summary>
    /// Sends what a ring heals on a kill through the run's sustain budget.
    /// </summary>
    /// <remarks>
    /// The vampire ring is the one that matters - two health a kill, bought for seven
    /// hundred gold - and on its own it was fine. The keystones changed how many kills
    /// a swing is worth, which turned it into thirty health a swing without the ring
    /// changing at all.
    ///
    /// The heal is undone and handed back through <see cref="Sustain"/> rather than
    /// prevented, so vanilla keeps deciding which rings heal and by how much. A
    /// transpiler over this switch would be a copy of the base game's table to keep in
    /// step with, for no gain.
    /// </remarks>
    [HarmonyPatch(typeof(Ring), nameof(Ring.onMonsterSlay))]
    internal class RingOnMonsterSlayPatch
    {
        public static void Prefix(Farmer who, out int __state)
        {
            __state = who?.health ?? 0;
        }

        public static void Postfix(Farmer who, int __state)
        {
            if (who is null || who != Game1.player)
                return;

            int gained = who.health - __state;
            if (gained <= 0)
                return;

            who.health = __state;
            Sustain.Heal(gained);
        }
    }
}

using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewRoguelike.VirtualProperties;
using StardewValley;
using StardewValley.Monsters;
using System;
using System.Collections.Generic;

namespace StardewRoguelike.Patches
{
    /// <summary>
    /// Makes a monster's hit-invincibility window belong to the player who caused it.
    /// </summary>
    /// <remarks>
    /// See <see cref="MonsterInvincibleUntil"/> for why: the vanilla field is local to
    /// one machine, which hands networked parties a window each and leaves split-screen
    /// parties sharing one. Three patches, because the decision is spread over three
    /// methods - who is swinging, whether their window is open, and when it closes.
    ///
    /// The vanilla field is still written exactly as before. It is what drives the
    /// monster's own hit animation, and leaving it alone keeps every other thing that
    /// reads it - contact damage, parries - behaving the way it did. Only the question
    /// "may this blow land" is answered per player.
    /// </remarks>
    internal static class PerAttacker
    {
        /// <summary>
        /// Who is swinging, innermost last.
        ///
        /// A stack rather than one field because these calls nest: the keystones
        /// spread a blow onto a target's neighbours by calling damageMonster again
        /// from inside the postfix of the blow that caused it, and a blast resolves
        /// its kills the same way. One field would be cleared by the inner call and
        /// leave the outer one anonymous.
        /// </summary>
        private static readonly List<Farmer> swinging = new();

        /// <summary>The player whose blow is being resolved, if this is a blow at all.</summary>
        public static Farmer? Current => swinging.Count > 0 ? swinging[^1] : null;

        public static void Push(Farmer? who)
        {
            if (who is not null)
                swinging.Add(who);
        }

        public static void Pop(Farmer? who)
        {
            if (who is null || swinging.Count == 0)
                return;

            // Searched from the top rather than popped blind, so an unbalanced call
            // cannot leave somebody else's name on the stack for the rest of the run.
            int last = swinging.LastIndexOf(who);
            if (last >= 0)
                swinging.RemoveAt(last);
        }

        /// <summary>Forgets everyone. Called when a run ends.</summary>
        public static void Reset()
        {
            swinging.Clear();
        }

        private static double Now =>
            Game1.currentGameTime?.TotalGameTime.TotalMilliseconds ?? 0;

        /// <summary>Whether <paramref name="who"/> is still inside their own window.</summary>
        public static bool IsClosedFor(Monster monster, Farmer who)
        {
            return monster.get_MonsterInvincibleUntil()
                .TryGetValue(who.UniqueMultiplayerID, out double until) && Now < until;
        }

        /// <summary>Shuts <paramref name="who"/> out of this monster for a while.</summary>
        public static void Close(Monster monster, Farmer who, int milliseconds)
        {
            monster.get_MonsterInvincibleUntil()[who.UniqueMultiplayerID] =
                Now + milliseconds;
        }
    }

    /// <summary>Names the player whose blow is being resolved, for the two below.</summary>
    [HarmonyPatch(
        typeof(GameLocation),
        nameof(GameLocation.damageMonster),
        new Type[] { typeof(Rectangle), typeof(int), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(float), typeof(float), typeof(bool), typeof(Farmer), typeof(bool) })
    ]
    internal class DamageMonsterAttackerPatch
    {
        public static void Prefix(Farmer who)
        {
            PerAttacker.Push(who);
        }

        /// <summary>
        /// Runs even when another prefix has skipped the method, which is what keeps
        /// the stack balanced however the rest of the mod's patches behave.
        /// </summary>
        public static void Postfix(Farmer who)
        {
            PerAttacker.Pop(who);
        }
    }

    /// <summary>Answers the invincibility test for the player who is swinging.</summary>
    [HarmonyPatch(typeof(Monster), nameof(Monster.isInvincible))]
    internal class MonsterIsInvinciblePatch
    {
        public static bool Prefix(Monster __instance, ref bool __result)
        {
            // Nobody is mid-swing, so this is one of the other callers - contact
            // damage deciding whether the monster may hurt the player, say - and it
            // still wants the shared answer it has always had.
            if (PerAttacker.Current is not Farmer who)
                return true;

            __result = PerAttacker.IsClosedFor(__instance, who);
            return false;
        }
    }

    /// <summary>Records the window against the player who earned it.</summary>
    [HarmonyPatch(typeof(Monster), nameof(Monster.setInvincibleCountdown))]
    internal class MonsterSetInvincibleCountdownPatch
    {
        public static void Prefix(Monster __instance, int time)
        {
            if (PerAttacker.Current is Farmer who)
                PerAttacker.Close(__instance, who, time);
        }
    }
}

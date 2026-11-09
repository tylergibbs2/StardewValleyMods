using StardewModdingAPI.Utilities;
using System.Collections.Generic;

namespace StardewRoguelike
{
    /// <summary>
    /// Where a run's power came from: which floor gave which perk, and which took
    /// which curse.
    /// </summary>
    /// <remarks>
    /// Nothing recorded this before. The perk list is a count of what is held and
    /// the curse list is a set of what is carried, and neither remembers where it
    /// was picked up - which is enough to play with and not enough to look back on.
    /// A run map that can only say "you have Volatile II" is a readout; one that can
    /// say "you took it at the shrine on floor seven" is a history.
    ///
    /// Per screen, because every entry is one player's: two local players walk the
    /// same floors and come out of them with different builds.
    ///
    /// Not synced, and not saved. It describes the run in progress to the person
    /// playing it, and there is nobody else who needs to agree about it.
    /// </remarks>
    internal static class RunLog
    {
        public enum EventKind
        {
            /// <summary>A perk was taken, at the depth it reached.</summary>
            PerkTaken,

            /// <summary>A curse was put on the player.</summary>
            CurseGained,

            /// <summary>A curse was taken off them.</summary>
            CurseLifted
        }

        /// <summary>One thing that happened to a player on one floor.</summary>
        /// <param name="Kind">What sort of thing it was.</param>
        /// <param name="Which">The perk or curse, as its enum value.</param>
        /// <param name="Depth">For a perk, the level it reached. Otherwise zero.</param>
        public readonly record struct Entry(EventKind Kind, int Which, int Depth);

        private static readonly PerScreen<Dictionary<int, List<Entry>>> ScreenLog = new(() => new());

        private static Dictionary<int, List<Entry>> Log => ScreenLog.Value;

        private static void Record(Entry entry)
        {
            // The floor the player is standing on when it happens. A perk taken at a
            // shrine belongs to the shrine's floor, which is where they are.
            int level = Roguelike.CurrentLevel;
            if (level <= 0)
                return;

            if (!Log.TryGetValue(level, out List<Entry>? entries))
            {
                entries = new();
                Log[level] = entries;
            }

            entries.Add(entry);
        }

        public static void PerkTaken(Perks.PerkType perk, int depth) =>
            Record(new(EventKind.PerkTaken, (int)perk, depth));

        public static void CurseGained(CurseType curse) =>
            Record(new(EventKind.CurseGained, (int)curse, 0));

        public static void CurseLifted(CurseType curse) =>
            Record(new(EventKind.CurseLifted, (int)curse, 0));

        /// <summary>What happened on <paramref name="level"/>, in the order it did.</summary>
        public static IReadOnlyList<Entry> For(int level) =>
            Log.TryGetValue(level, out List<Entry>? entries) ? entries : System.Array.Empty<Entry>();

        /// <summary>Whether anything at all happened on that floor.</summary>
        public static bool Any(int level) => For(level).Count > 0;

        /// <summary>Forgets the run. Called when one begins.</summary>
        public static void Reset() => ScreenLog.Value = new();
    }
}

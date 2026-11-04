using StardewModdingAPI;
using StardewModdingAPI.Events;
using System;
using System.Collections.Generic;

namespace StardewRoguelike.Mode
{
    /// <summary>
    /// Tracks whether the player is in a roguelike session or a vanilla one, and
    /// applies or undoes every global change the mod makes when that flips.
    ///
    /// The mod used to be a total conversion: it rebuilt the title screen, hijacked
    /// the load menu and patched the game the moment it loaded, so installing it
    /// meant giving up vanilla Stardew. Everything that takes the game over now
    /// lives behind this switch.
    /// </summary>
    internal static class RoguelikeMode
    {
        private static readonly List<IModeEffect> Effects = new();

        /// <summary>Whether the roguelike currently owns the game.</summary>
        public static bool IsActive { get; private set; }

        /// <summary>The state we have been asked to reach, once it is safe to do so.</summary>
        private static bool? requested;

        public static void Register(IModeEffect effect) => Effects.Add(effect);

        /// <summary>
        /// Asks for the mode to change. The switch does not happen here.
        ///
        /// Unpatching a method while it is running tears the ground out from under
        /// the call stack, and the places that want to leave the roguelike - exiting
        /// to title, finishing a run - are themselves reached from patched code. So
        /// the request is recorded and carried out from the update loop, where no
        /// patch of ours is on the stack.
        /// </summary>
        public static void Request(bool active)
        {
            if (active == IsActive && requested is null)
                return;

            requested = active;
        }

        /// <summary>Applies a pending mode change. Hooked to the update loop.</summary>
        public static void UpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (requested is not bool wanted)
                return;

            requested = null;
            if (wanted == IsActive)
                return;

            Apply(wanted);
        }

        /// <summary>
        /// Switches immediately. Only safe when no patched method is executing;
        /// callers that cannot promise that want <see cref="Request"/> instead.
        /// </summary>
        public static void Apply(bool active)
        {
            if (active == IsActive)
                return;

            ModEntry.ModMonitor.Log($"Roguelike mode -> {(active ? "on" : "off")}", LogLevel.Debug);

            // Before the effects run, not after. An effect can make the game ask us
            // something straight away - invalidating a cached asset makes it reload
            // that asset within the call - and anything that asks which mode we are
            // in has to be told the new one. Setting this afterwards meant the asset
            // handler declined to apply the mod's overlays during activation and
            // cached the vanilla art instead, so the sheets never carried mod art
            // and the game drew whatever vanilla had at those coordinates.
            IsActive = active;

            // On the way out, undo in reverse: effects are registered cheapest-first
            // and the later ones may rely on the earlier ones still being in place.
            IEnumerable<IModeEffect> order = active ? Effects : Reversed(Effects);

            foreach (IModeEffect effect in order)
            {
                try
                {
                    if (active)
                        effect.Activate();
                    else
                        effect.Deactivate();
                }
                catch (Exception ex)
                {
                    // Keep going. A half-applied mode is bad, but stopping here
                    // guarantees it, and the remaining effects are independent.
                    ModEntry.ModMonitor.Log(
                        $"Mode effect '{effect.Name}' failed to {(active ? "activate" : "deactivate")}: {ex}",
                        LogLevel.Error);
                }
            }
        }

        private static IEnumerable<IModeEffect> Reversed(List<IModeEffect> list)
        {
            for (int i = list.Count - 1; i >= 0; i--)
                yield return list[i];
        }
    }
}

using StardewModdingAPI.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace StardewRoguelike.Mode
{
    /// <summary>
    /// Clears the run state the mod keeps outside the save file.
    ///
    /// Two kinds, both found by reflection rather than by a hand-written list, so
    /// state added later is cleaned up without anyone editing this class:
    ///
    ///   - PerScreen fields, which hold the current run (floor, RNG, minigame) and
    ///     outlive a return to title
    ///   - the ConditionalWeakTables in VirtualProperties, which hang mod data off
    ///     game objects. Those mostly empty themselves once the farmer and the
    ///     locations are collected, but "mostly" and "once" are both the garbage
    ///     collector's business, and a vanilla save should not be waiting on it.
    /// </summary>
    internal sealed class StateEffect : IModeEffect
    {
        public string Name => "state";

        /// <summary>Nothing to set up: a run initialises its own state as it starts.</summary>
        public void Activate() { }

        public void Deactivate()
        {
            // The sweep below reaches every PerScreen field and every virtual
            // property table, which between them hold everything the mod keeps for a
            // run bar one thing: the spectator toolbar is a menu sitting in
            // Game1.onScreenMenus, and only SpectatorMode knows how to take it out.
            SpectatorMode.Reset();

            foreach (object container in StateContainers())
            {
                switch (container)
                {
                    case null:
                        continue;

                    default:
                        Clear(container);
                        break;
                }
            }
        }

        private static void Clear(object container)
        {
            Type type = container.GetType();
            if (!type.IsGenericType)
                return;

            Type definition = type.GetGenericTypeDefinition();

            if (definition == typeof(ConditionalWeakTable<,>))
            {
                type.GetMethod("Clear")?.Invoke(container, Array.Empty<object>());
                return;
            }

            if (definition == typeof(PerScreen<>))
            {
                // ResetAllScreens, not the Value setter. Value only ever reaches the
                // screen the mode switch happened to run under, so in split-screen
                // the other screen kept a finished run's floor, perks and menus. It
                // also drops each screen back to the field's own factory rather than
                // to null or zero, which is what the author declared "no run" to
                // mean - PerScreen<int> TrueMaxHP reads the farmer's max health
                // again, instead of becoming nought.
                type.GetMethod("ResetAllScreens")?.Invoke(container, Array.Empty<object>());
            }
        }

        /// <summary>Every static PerScreen or ConditionalWeakTable the mod declares.</summary>
        private static IEnumerable<object?> StateContainers()
        {
            const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                FieldInfo[] fields;
                try
                {
                    fields = type.GetFields(flags);
                }
                catch
                {
                    continue;
                }

                foreach (FieldInfo field in fields)
                {
                    if (!field.FieldType.IsGenericType)
                        continue;

                    Type definition = field.FieldType.GetGenericTypeDefinition();
                    if (definition != typeof(PerScreen<>) && definition != typeof(ConditionalWeakTable<,>))
                        continue;

                    object? value = null;
                    try
                    {
                        value = field.GetValue(null);
                    }
                    catch
                    {
                        // A static field whose initialiser throws outside a run is
                        // not state worth clearing.
                    }

                    yield return value;
                }
            }
        }
    }
}

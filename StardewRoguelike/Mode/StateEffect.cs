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
            ReleaseRunReferences();

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

        /// <summary>
        /// Drops the plain statics that outlive a run.
        ///
        /// The reflection below only finds PerScreen fields and the virtual property
        /// tables. These are ordinary statics holding live objects - menus built for
        /// the last run, and a Farmer from a save that has since been unloaded - and
        /// nothing else lets go of them when the mode turns off.
        /// </summary>
        private static void ReleaseRunReferences()
        {
            Merchant.CurrentShop = null;
            Merchant.CurrentSeedShop = null;
            Merchant.CurrentHatBoard = null;
            Merchant.CurseToAdd = null;

            Perks.CurrentMenu = null;
            ForgeFloor.CurrentForge = null;

            SpectatorMode.Reset();

            ModEntry.Invincible = false;
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
                // Assigning the default re-seeds every screen's copy, which is what
                // "no run in progress" looks like for all of these.
                PropertyInfo? value = type.GetProperty("Value");
                if (value?.CanWrite == true)
                {
                    Type held = type.GetGenericArguments()[0];
                    value.SetValue(container, held.IsValueType ? Activator.CreateInstance(held) : null);
                }
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

using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using System.IO;
using System.Reflection;

namespace StardewRoguelike.Mode
{
    /// <summary>
    /// Takes the player from the title screen into a roguelike run.
    ///
    /// The mode has to be on before the save is read, or the game loads the real
    /// mine instead of the lobby and resolves every item against unpatched rules.
    /// Since the switch is deferred to a tick boundary, entering is a two step
    /// affair: ask for the mode, then load once it is actually on.
    /// </summary>
    internal static class RoguelikeEntry
    {
        private static bool loadPending;

        /// <summary>
        /// The player's window settings, taken before the bundled save replaces them.
        /// </summary>
        public static DisplaySettings? CapturedDisplay { get; private set; }

        /// <summary>Begins entering a run. Safe to call from inside a patch.</summary>
        public static void Begin()
        {
            // Taken now, while the settings on screen are still the player's own.
            CapturedDisplay = DisplaySettings.Capture();

            loadPending = true;
            RoguelikeMode.Request(true);
        }

        /// <summary>Ticks still to wait before re-asserting the player's window.</summary>
        private static int displayTicks = -1;

        /// <summary>
        /// Re-applies the captured window settings over the next second.
        ///
        /// Applying them once as the save finishes loading is not enough: the game
        /// is still settling its own window at that point, and the settings from the
        /// save win. Resizing the window by hand fixed it, which is the giveaway that
        /// the values were right all along and only arrived too early.
        /// </summary>
        public static void ReassertDisplay()
        {
            displayTicks = 0;
        }

        /// <summary>Leaves the run and hands the game back to vanilla.</summary>
        public static void Leave()
        {
            loadPending = false;
            RoguelikeMode.Request(false);
        }

        /// <summary>
        /// Runs after <see cref="RoguelikeMode.UpdateTicked"/>, so by the time this
        /// sees the mode on, every patch and asset swap is already in place.
        /// </summary>
        public static void UpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (displayTicks >= 0)
            {
                displayTicks++;

                // Three goes, spread over about a second of game time.
                if (displayTicks is 1 or 20 or 60)
                    CapturedDisplay?.Apply();

                if (displayTicks > 60)
                    displayTicks = -1;
            }

            if (!loadPending || !RoguelikeMode.IsActive)
                return;

            loadPending = false;
            LoadBundledSave();
        }

        /// <summary>
        /// Loads the save the mod ships with. It lives inside the mod folder rather
        /// than the player's save directory, which is why it never shows up in their
        /// load menu alongside their own farms.
        /// </summary>
        public static void LoadBundledSave()
        {
            string path = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                $@"assets/Saves/{Constants.SaveFile}/SaveGameInfo");

            ModEntry.ModMonitor.Log($"Loading roguelike save from {path}", LogLevel.Debug);

            SaveGame.Load(path);
            Game1.exitActiveMenu();
        }
    }
}

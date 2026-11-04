using StardewValley;

namespace StardewRoguelike.Mode
{
    /// <summary>
    /// The parts of <see cref="Options"/> that describe the player's window rather
    /// than how they like to play.
    ///
    /// The save the mod ships with carries its author's display settings - a zoom of
    /// 0.75 against a UI scale of 1 - and loading it applies them. The world then
    /// renders at three quarters size inside a full size window while the HUD lays
    /// itself out across the whole thing, which puts the money box off the right
    /// edge, pins the toolbar to the left and stops menus being centred. These are
    /// captured before the save loads and put back afterwards.
    /// </summary>
    internal readonly struct DisplaySettings
    {
        private readonly bool fullscreen;
        private readonly bool windowedBorderless;
        private readonly float singlePlayerZoom;
        private readonly float localCoopZoom;
        private readonly float singlePlayerUIScale;
        private readonly float localCoopUIScale;

        private DisplaySettings(Options options)
        {
            fullscreen = options.fullscreen;
            windowedBorderless = options.windowedBorderlessFullscreen;
            singlePlayerZoom = options.singlePlayerBaseZoomLevel;
            localCoopZoom = options.localCoopBaseZoomLevel;
            singlePlayerUIScale = options.singlePlayerDesiredUIScale;
            localCoopUIScale = options.localCoopDesiredUIScale;
        }

        public static DisplaySettings Capture()
        {
            DisplaySettings captured = new(Game1.options);
            ModEntry.ModMonitor.Log(
                $"captured display: zoom={captured.singlePlayerZoom} uiScale={captured.singlePlayerUIScale} " +
                $"fullscreen={captured.fullscreen} borderless={captured.windowedBorderless}",
                StardewModdingAPI.LogLevel.Debug);
            return captured;
        }

        /// <summary>Puts these settings back and makes the game act on them.</summary>
        public void Apply()
        {
            Options options = Game1.options;

            options.fullscreen = fullscreen;
            options.windowedBorderlessFullscreen = windowedBorderless;
            options.singlePlayerBaseZoomLevel = singlePlayerZoom;
            options.localCoopBaseZoomLevel = localCoopZoom;
            options.singlePlayerDesiredUIScale = singlePlayerUIScale;
            options.localCoopDesiredUIScale = localCoopUIScale;

            ModEntry.ModMonitor.Log(
                $"applying display: zoom={singlePlayerZoom} uiScale={singlePlayerUIScale}; " +
                $"before refresh viewport={Game1.viewport.Width}x{Game1.viewport.Height} " +
                $"ui={Game1.uiViewport.Width}x{Game1.uiViewport.Height}",
                StardewModdingAPI.LogLevel.Debug);

            // Assigning the fields resizes nothing on its own.
            Game1.game1.refreshWindowSettings();

            ModEntry.ModMonitor.Log(
                $"after refresh: zoomLevel={Game1.options.zoomLevel} uiScale={Game1.options.uiScale} " +
                $"viewport={Game1.viewport.Width}x{Game1.viewport.Height} " +
                $"ui={Game1.uiViewport.Width}x{Game1.uiViewport.Height}",
                StardewModdingAPI.LogLevel.Debug);
        }
    }
}

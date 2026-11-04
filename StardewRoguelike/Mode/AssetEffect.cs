using StardewModdingAPI;

namespace StardewRoguelike.Mode
{
    /// <summary>
    /// Turns the mod's asset replacements on and off.
    ///
    /// The handler itself stays subscribed either way - it just declines to replace
    /// anything in a vanilla session - because SMAPI caches what a mod loaded and
    /// the cache is what actually has to change. Maps/Mine is the one that matters
    /// most: in roguelike mode it is the lobby, and in a vanilla save it has to be
    /// the real mine again.
    /// </summary>
    internal sealed class AssetEffect : IModeEffect
    {
        public string Name => "assets";

        public void Activate() => Invalidate();

        public void Deactivate() => Invalidate();

        private static void Invalidate()
        {
            foreach (string asset in ContentManager.ModeScopedAssets)
                ModEntry.GameContentHelper.InvalidateCache(asset);
        }
    }
}

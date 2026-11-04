namespace StardewRoguelike.Mode
{
    /// <summary>
    /// One change the roguelike makes to the vanilla game.
    ///
    /// Every implementation has to undo itself completely. Whatever Deactivate
    /// leaves behind is what leaks into the next vanilla session the player loads,
    /// so an effect that cannot be undone does not belong here - it belongs in the
    /// always-active set instead.
    /// </summary>
    internal interface IModeEffect
    {
        /// <summary>A short name for logging which effect failed.</summary>
        string Name { get; }

        void Activate();

        void Deactivate();
    }
}

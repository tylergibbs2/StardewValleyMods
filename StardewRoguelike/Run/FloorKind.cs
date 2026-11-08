namespace StardewRoguelike.Run
{
    /// <summary>
    /// What a floor is. Replaces the chain of ShouldDoXFloor predicates that used to
    /// decide this one floor at a time, each of them rolling FloorRng and reading
    /// whichever mines happened to be loaded.
    /// </summary>
    public enum FloorKind
    {
        /// <summary>A plain mine layout. Rocks, monsters, an exit.</summary>
        Normal,

        /// <summary>A combat challenge floor: hold the point, clear the waves, race.</summary>
        Arena,

        /// <summary>A chest floor. Loot with no fight attached.</summary>
        Vault,

        /// <summary>A forge floor. One weapon enchantment.</summary>
        Forge,

        /// <summary>The hot spring. Heals, and does nothing else.</summary>
        Spring,

        /// <summary>A shrine. Takes a curse, gives a perk.</summary>
        Shrine,

        /// <summary>A merchant floor. Forced every sixth floor; never offered as a choice.</summary>
        Merchant,

        /// <summary>A boss floor. Forced before each merchant; never offered as a choice.</summary>
        Boss,

        /// <summary>
        /// A mine layout with the lights out. Thicker stone and more of it worth
        /// breaking, and more of everything else too.
        ///
        /// Last on purpose: the value is what the route is stored as, so the ones
        /// above it have to keep the numbers they already have.
        /// </summary>
        DarkMineshaft,

        /// <summary>
        /// A cauldron. Takes a curse away, and gives nothing else.
        ///
        /// The other half of the shrine: one sells a perk for a curse and this buys
        /// the curse back, so taking a curse is a decision that can be undone at the
        /// cost of a floor rather than a mark carried to the end of the run.
        /// </summary>
        Cauldron
    }
}

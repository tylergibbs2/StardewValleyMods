using System;

namespace StardewRoguelike.Mode
{
    /// <summary>
    /// Marks a Harmony patch that must stay applied even when the player is in a
    /// vanilla session.
    ///
    /// Only the patches that implement routing belong here: the title screen needs
    /// its roguelike button, the load menu needs to know whether to hand over to the
    /// roguelike save, and the co-op menus need to negotiate the mode with a host.
    /// Everything else is mode scoped by default, so a new gameplay patch is
    /// correct without anyone having to remember this exists.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    internal sealed class AlwaysActiveAttribute : Attribute
    {
    }
}

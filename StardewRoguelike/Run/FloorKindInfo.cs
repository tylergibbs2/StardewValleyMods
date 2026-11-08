using Microsoft.Xna.Framework;

namespace StardewRoguelike.Run
{
    /// <summary>
    /// How each <see cref="FloorKind"/> is presented at an exit.
    ///
    /// The point of the choice is that it can be read before it is taken, so every
    /// kind needs a name, a line saying what it costs or gives, and an icon.
    /// </summary>
    internal static class FloorKindInfo
    {
        public static string Name(FloorKind kind) => kind switch
        {
            FloorKind.Normal => I18n.ExitChoice_Normal(),
            FloorKind.Arena => I18n.ExitChoice_Arena(),
            FloorKind.Vault => I18n.ExitChoice_Vault(),
            FloorKind.Forge => I18n.ExitChoice_Forge(),
            FloorKind.Spring => I18n.ExitChoice_Spring(),
            FloorKind.Merchant => I18n.ExitChoice_Merchant(),
            _ => I18n.ExitChoice_Boss()
        };

        public static string Description(FloorKind kind) => kind switch
        {
            FloorKind.Normal => I18n.ExitChoice_NormalDesc(),
            FloorKind.Arena => I18n.ExitChoice_ArenaDesc(),
            FloorKind.Vault => I18n.ExitChoice_VaultDesc(),
            FloorKind.Forge => I18n.ExitChoice_ForgeDesc(),
            FloorKind.Spring => I18n.ExitChoice_SpringDesc(),
            FloorKind.Merchant => I18n.ExitChoice_MerchantDesc(),
            _ => I18n.ExitChoice_BossDesc()
        };

        /// <summary>A source rectangle on Game1.mouseCursors to stand for the kind.</summary>
        /// <remarks>
        /// Vanilla art on purpose: ContentManager only overlays the regions the mod
        /// draws its own art from, and none of these fall inside one, so they stay
        /// whatever the game shipped.
        /// </remarks>
        public static Rectangle Icon(FloorKind kind) => kind switch
        {
            FloorKind.Normal => new Rectangle(0, 428, 10, 10),      // pickaxe-ish
            FloorKind.Arena => new Rectangle(120, 428, 10, 10),     // crossed blades
            FloorKind.Vault => new Rectangle(130, 428, 10, 10),     // chest
            FloorKind.Forge => new Rectangle(150, 428, 10, 10),     // anvil
            FloorKind.Spring => new Rectangle(140, 428, 10, 10),    // droplet
            FloorKind.Merchant => new Rectangle(30, 428, 10, 10),   // coin
            _ => new Rectangle(140, 412, 10, 10)                    // skull
        };
    }
}

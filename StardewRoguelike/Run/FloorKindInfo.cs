using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

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
            FloorKind.DarkMineshaft => I18n.ExitChoice_DarkMineshaft(),
            FloorKind.Cauldron => I18n.ExitChoice_Cauldron(),
            FloorKind.Arena => I18n.ExitChoice_Arena(),
            FloorKind.Vault => I18n.ExitChoice_Vault(),
            FloorKind.Forge => I18n.ExitChoice_Forge(),
            FloorKind.Spring => I18n.ExitChoice_Spring(),
            FloorKind.Shrine => I18n.ExitChoice_Shrine(),
            FloorKind.Merchant => I18n.ExitChoice_Merchant(),
            _ => I18n.ExitChoice_Boss()
        };

        /// <summary>
        /// What the floor at <paramref name="level"/> gives, named where it can be.
        /// </summary>
        /// <remarks>
        /// A shrine says which two perks it is holding. Knowing the kind of a floor
        /// is enough to choose between a fight and a heal, but it is not enough to
        /// build anything: a player steering towards something has to be able to see
        /// it from the floor above, or the route cannot serve the build.
        /// </remarks>
        public static string Description(FloorKind kind, int level)
        {
            if (kind == FloorKind.Vault)
                return TreasureDescription(level);

            if (kind != FloorKind.Shrine)
                return Description(kind);

            (Perks.PerkType? left, Perks.PerkType? right) = Perks.GetShrineOffer(level);

            if (left is null)
                return Description(kind);

            if (right is null)
                return I18n.ExitChoice_ShrineOfferOne(perk: Perks.GetOfferDisplayName(left.Value));

            return I18n.ExitChoice_ShrineOffer(
                first: Perks.GetOfferDisplayName(left.Value),
                second: Perks.GetOfferDisplayName(right.Value));
        }

        /// <summary>
        /// What a treasure floor is offering, which depends on which of them it is.
        /// </summary>
        /// <remarks>
        /// The plain chest names the thing it is holding, because that can be known.
        /// The other three name the shape of the gamble instead: what is in them is
        /// decided when they are opened, and saying so is the honest version of
        /// naming it. Between them the exit can tell the player whether this is a
        /// known quantity, a price, or a roll.
        /// </remarks>
        private static string TreasureDescription(int level)
        {
            ChallengeFloor.ChallengeType? treasure = RunRoute.VaultFor(level);

            if (treasure == ChallengeFloor.ChallengeType.DwarfChests)
                return I18n.ExitChoice_VaultDwarf();

            if (treasure == ChallengeFloor.ChallengeType.GambaChests)
                return I18n.ExitChoice_VaultGamba();

            if (treasure == ChallengeFloor.ChallengeType.PickAPath)
                return I18n.ExitChoice_VaultDoors();

            Item? item = ChestFloor.ItemFor(level);

            return item is null
                ? Description(FloorKind.Vault)
                : I18n.ExitChoice_VaultHolds(item: item.DisplayName);
        }

        public static string Description(FloorKind kind) => kind switch
        {
            FloorKind.Normal => I18n.ExitChoice_NormalDesc(),
            FloorKind.DarkMineshaft => I18n.ExitChoice_DarkMineshaftDesc(),
            FloorKind.Cauldron => I18n.ExitChoice_CauldronDesc(),
            FloorKind.Arena => I18n.ExitChoice_ArenaDesc(),
            FloorKind.Vault => I18n.ExitChoice_VaultDesc(),
            FloorKind.Forge => I18n.ExitChoice_ForgeDesc(),
            FloorKind.Spring => I18n.ExitChoice_SpringDesc(),
            FloorKind.Shrine => I18n.ExitChoice_ShrineDesc(),
            FloorKind.Merchant => I18n.ExitChoice_MerchantDesc(),
            _ => I18n.ExitChoice_BossDesc()
        };

        /// <summary>
        /// The fire quartz, which is the crystal the deep mines are known for and so
        /// says what a dark mineshaft is worth walking into.
        /// </summary>
        private const int FireQuartz = 82;

        /// <summary>The sheet the kind's icon is drawn from.</summary>
        /// <remarks>
        /// The small icons all live on the cursors sheet. A kind whose whole pitch is
        /// what comes out of the rock is better served by the rock itself, so it takes
        /// its icon off the object sheet instead.
        /// </remarks>
        public static Texture2D IconTexture(FloorKind kind) => kind switch
        {
            FloorKind.DarkMineshaft => Game1.objectSpriteSheet,

            // Its own art, which is the only picture of a cauldron the mod has and
            // says what the floor is better than anything on the cursor sheet.
            FloorKind.Cauldron => Game1.content.Load<Texture2D>(CauldronTexture),
            _ => Game1.mouseCursors
        };

        /// <summary>The cauldron's own sheet, which the terrain feature draws from.</summary>
        private const string CauldronTexture = "TerrainFeatures\\CleansingCauldron";

        /// <summary>A source rectangle on the kind's icon sheet to stand for it.</summary>
        /// <remarks>
        /// Read off the sheet and cross-checked against where the base game draws
        /// each one, rather than guessed: the row at y=428 is a ten-pixel grid of
        /// small icons, and the chests sit just above it.
        ///
        /// Vanilla art on purpose. ContentManager only overlays the regions the mod
        /// draws its own art from, and none of these fall inside one, so they stay
        /// whatever the game shipped.
        /// </remarks>
        public static Rectangle Icon(FloorKind kind) => kind switch
        {
            FloorKind.Normal => new Rectangle(30, 428, 10, 10),     // pickaxe
            FloorKind.DarkMineshaft =>
                Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, FireQuartz, 16, 16),
            FloorKind.Cauldron => new Rectangle(0, 0, 48, 48),
            FloorKind.Arena => new Rectangle(120, 428, 10, 10),     // sword
            FloorKind.Vault => new Rectangle(127, 412, 10, 11),     // chest
            FloorKind.Forge => new Rectangle(70, 428, 10, 10),      // iron bar
            FloorKind.Spring => new Rectangle(0, 428, 10, 10),      // health cross
            FloorKind.Shrine => new Rectangle(150, 428, 10, 10),    // potion
            FloorKind.Merchant => new Rectangle(147, 412, 10, 11),  // chest, open
            _ => new Rectangle(140, 428, 10, 10)                    // skull
        };
    }
}

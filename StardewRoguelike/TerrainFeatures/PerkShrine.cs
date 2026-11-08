using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;
using System;

namespace StardewRoguelike.TerrainFeatures
{
    /// <summary>
    /// Offers power for a price: take a curse, choose a perk.
    ///
    /// The deliberate inverse of <see cref="CleansingCauldron"/>, which spends a
    /// curse to grant a perk. Between them a curse is a currency rather than only a
    /// penalty - the shrine sells you one, the cauldron buys it back - and perks stop
    /// arriving on a fixed timetable, since reaching a shrine is a routing decision.
    /// </summary>
    internal class PerkShrine : TerrainFeature
    {
        /// <summary>
        /// The Wicked Statue, drawn straight from the game's big craftable sheet.
        ///
        /// Vanilla art rather than anything the mod ships: it is already the game's
        /// own dark statue, which is exactly what a shrine selling power for a curse
        /// should look like, and it means no new sprite to draw or asset to load. The
        /// mod's other statues work the same way - the healing bear on a pick-a-path
        /// floor is built from townInterior tiles.
        /// </summary>
        private const int WickedStatue = 83;

        private bool used = false;

        /// <summary>Nothing to tick: the shrine only reacts to being used.</summary>
        public PerkShrine() : base(needsTick: false) { }

        public override Rectangle getBoundingBox()
        {
            return new((int)Tile.X * 64, (int)Tile.Y * 64, 64, 64);
        }

        /// <summary>
        /// Puts a shrine on <paramref name="mine"/>, near where the player arrives.
        ///
        /// Near the entrance on purpose. The whole point of choosing a shrine at the
        /// exit is that you get one - having to search the floor for it would put
        /// back the hunting that choosing an exit is meant to remove.
        /// </summary>
        public static void PlaceIn(MineShaft mine)
        {
            Vector2 entrance = mine.tileBeneathLadder;

            for (int radius = 2; radius <= 12; radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        if (Math.Abs(dx) != radius && Math.Abs(dy) != radius)
                            continue;

                        Vector2 tile = new(entrance.X + dx, entrance.Y + dy);
                        if (!HasRoomFor(mine, tile))
                            continue;

                        mine.terrainFeatures.Add(tile, new PerkShrine());
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Whether the shrine fits at this tile.
        ///
        /// It stands on one tile and is drawn two tall, so the tile above only has to
        /// be free of another shrine, not walkable.
        /// </summary>
        private static bool HasRoomFor(MineShaft mine, Vector2 tile)
        {
            if (mine.terrainFeatures.ContainsKey(tile))
                return false;

            if (!mine.isTileClearForMineObjects(tile))
                return false;

            return !mine.terrainFeatures.ContainsKey(new Vector2(tile.X, tile.Y - 1));
        }

        public override bool performUseAction(Vector2 tileLocation)
        {
            if (used)
                return false;

            if (Perks.HasAllPerks())
            {
                Game1.drawObjectDialogue(I18n.Shrine_NothingToOffer());
                return false;
            }

            // The price. Running out of curses to give does not block the trade: the
            // player has already paid for every one they are carrying.
            CurseType? curse = Curse.GetRandomUniqueCurse();
            if (curse.HasValue)
                Curse.AddCurse(curse.Value);

            used = true;
            Game1.playSound("debuffSpell");

            // A choice rather than a random grant, which is what the merchant floors
            // used to give and is the more interesting half of taking a perk.
            Perks.CurrentMenu = new()
            {
                isActive = true,
                informationUp = true
            };
            Game1.activeClickableMenu = Perks.CurrentMenu;

            return true;
        }

        public override bool isPassable(Character? c = null)
        {
            return false;
        }

        public override bool isActionable()
        {
            return !used;
        }

        public override void draw(SpriteBatch spriteBatch)
        {
            // Drawn from the tile above, since a big craftable is two tiles tall and
            // stands on the lower one.
            Vector2 position = Game1.GlobalToLocal(
                Game1.viewport, new Vector2(Tile.X * 64f, (Tile.Y - 1) * 64f));

            spriteBatch.Draw(
                Game1.bigCraftableSpriteSheet,
                position,
                StardewValley.Object.getSourceRectForBigCraftable(WickedStatue),
                used ? Color.Gray * 0.6f : Color.White,
                0f,
                Vector2.Zero,
                4f,
                SpriteEffects.None,
                (Tile.Y * 64f + 32f) / 10000f);
        }
    }
}

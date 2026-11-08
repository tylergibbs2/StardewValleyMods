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
        private string textureName = "TerrainFeatures\\CleansingCauldron";

        private Lazy<Texture2D> texture = null!;

        private bool used = false;

        public PerkShrine() : base(false)
        {
            ResetTexture();
        }

        protected void ResetTexture()
        {
            texture = new(new Func<Texture2D>(LoadTexture));
        }

        protected Texture2D LoadTexture()
        {
            return Game1.content.Load<Texture2D>(textureName);
        }

        public override Rectangle getBoundingBox()
        {
            return new((int)Tile.X * 64, (int)(Tile.Y + 1) * 64, 192, 128);
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

        /// <summary>Whether the shrine's three by two footprint fits at this tile.</summary>
        private static bool HasRoomFor(MineShaft mine, Vector2 tile)
        {
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    Vector2 covered = new(tile.X + x, tile.Y + y);

                    if (mine.terrainFeatures.ContainsKey(covered))
                        return false;

                    if (!mine.isTileClearForMineObjects(covered))
                        return false;
                }
            }

            return true;
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
            textureName += "Empty";
            ResetTexture();
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
            Vector2 position = Game1.GlobalToLocal(Game1.viewport, new Vector2(Tile.X * 64f, Tile.Y * 64f));

            spriteBatch.Draw(
                texture.Value,
                position,
                texture.Value.Bounds,
                Color.White,
                0f,
                Vector2.Zero,
                4f,
                SpriteEffects.None,
                (Tile.Y * 64f + 128f) / 10000f);
        }
    }
}

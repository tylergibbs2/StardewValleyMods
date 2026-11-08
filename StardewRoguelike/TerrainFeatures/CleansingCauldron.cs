using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;
using System;
using Microsoft.Xna.Framework;

namespace StardewRoguelike.TerrainFeatures
{
    internal class CleansingCauldron : TerrainFeature
    {
        private string textureName = "TerrainFeatures\\CleansingCauldron";

        private Lazy<Texture2D> texture = null!;

        private bool used = false;

        public CleansingCauldron() : base(false)
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
        /// Puts a cauldron on <paramref name="mine"/>, near where the player arrives.
        ///
        /// Near the entrance on purpose. A floor taken to undo something is not a
        /// floor anybody wants to search, and unlike the shrine it asks for no fight
        /// first, so there is nothing to walk past on the way.
        /// </summary>
        public static void PlaceIn(MineShaft mine)
        {
            if (mine.map is null)
                return;

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

                        mine.terrainFeatures.Add(tile, new CleansingCauldron());
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Whether a cauldron fits here.
        ///
        /// It is drawn three tiles wide and two tall, so the tiles around it have to
        /// be clear of anything else that stands on the floor.
        /// </summary>
        private static bool HasRoomFor(MineShaft mine, Vector2 tile)
        {
            for (int dx = 0; dx < 3; dx++)
            {
                for (int dy = 0; dy < 2; dy++)
                {
                    Vector2 part = new(tile.X + dx, tile.Y + dy);

                    if (mine.terrainFeatures.ContainsKey(part) || !mine.isTileClearForMineObjects(part))
                        return false;
                }
            }

            return true;
        }

        public override bool performUseAction(Vector2 tileLocation)
        {
            if (used || !Curse.HasAnyCurse())
                return false;

            used = true;
            textureName += "Empty";
            ResetTexture();
            Game1.playSound("glug");
            // Cleansing, and nothing else. It handed over a perk as well while it
            // was a room behind one of two doors, on a floor nobody could count on
            // reaching. As a floor of its own that is two rewards for no price, and
            // it would be strictly better than the shrine it is meant to be the
            // other half of: the shrine sells a perk for a curse, this buys the
            // curse back, and a run has to decide which it wants a floor for.
            Curse.RemoveRandomCurse();

            return true;
        }

        public override bool isPassable(Character? c = null)
        {
            return false;
        }

        public override bool isActionable()
        {
            return false;
        }

        public override void draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(texture.Value, Game1.GlobalToLocal(Game1.viewport, Tile * 64f), new Rectangle(0, 0, 48, 48), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (Tile.Y + 2f) * 64f / 10000f);
        }
    }
}

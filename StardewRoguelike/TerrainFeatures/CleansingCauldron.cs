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
        /// <summary>The cauldron with something in it.</summary>
        private const string FullTexture = "TerrainFeatures\\CleansingCauldron";

        /// <summary>The cauldron with nothing in it.</summary>
        private const string EmptyTexture = "TerrainFeatures\\CleansingCauldronEmpty";

        private string textureName = EmptyTexture;

        private Lazy<Texture2D> texture = null!;

        private bool used = false;

        /// <summary>Whether the floor has been told the cauldron filled.</summary>
        private bool announced = false;

        /// <summary>Ticks so it can notice the last monster dying.</summary>
        public CleansingCauldron() : base(needsTick: true)
        {
            ResetTexture();
        }

        /// <summary>
        /// Whether the cauldron will take a curse.
        ///
        /// The floor has to be cleared first, the same as the shrine's. Undoing a
        /// decision is worth a floor of work; otherwise a cauldron is a free walk to
        /// a pot, and the curse the shrine charged stops being a price at all.
        /// </summary>
        private bool Ready => !used && Location is MineShaft mine && mine.EnemyCount == 0;

        public override bool tickUpdate(GameTime time)
        {
            // Said once, when the last monster dies, and the pot fills to match. The
            // colour is the only thing telling the player it is waiting on anything.
            if (!used && !announced && Ready)
            {
                announced = true;
                textureName = FullTexture;
                ResetTexture();

                Game1.chatBox?.addMessage(I18n.Cauldron_Ready(), Color.Gold);
                Location?.localSound("bubbles");
            }

            return false;
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
        /// In the middle of the floor, like the shrine, and for the same reason: the
        /// floor has to be cleared before it will take anything, so the middle is
        /// where the fight ends up anyway.
        /// </summary>
        public static void PlaceIn(MineShaft mine)
        {
            if (mine.map is null)
                return;

            Vector2 middle = new(
                mine.Map.Layers[0].LayerWidth / 2,
                mine.Map.Layers[0].LayerHeight / 2);

            if (TryPlace(mine, middle))
                return;

            // Nothing clear in the middle still gets a cauldron rather than none,
            // since the floor was chosen for it.
            TryPlace(mine, mine.tileBeneathLadder);
        }

        private static bool TryPlace(MineShaft mine, Vector2 entrance)
        {
            for (int radius = 0; radius <= 12; radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        if (radius > 0 && Math.Abs(dx) != radius && Math.Abs(dy) != radius)
                            continue;

                        Vector2 tile = new(entrance.X + dx, entrance.Y + dy);
                        if (!HasRoomFor(mine, tile))
                            continue;

                        mine.terrainFeatures.Add(tile, new CleansingCauldron());
                        return true;
                    }
                }
            }

            return false;
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
            if (used)
                return false;

            if (!Ready)
            {
                // A line in the chat rather than a dialogue box, so being told no
                // does not stop the floor while something is still chasing you.
                Game1.chatBox?.addMessage(I18n.Cauldron_Dormant(), Color.Gold);
                return false;
            }

            if (!Curse.HasAnyCurse())
            {
                Game1.drawObjectDialogue(I18n.Cauldron_NothingToTake());
                return false;
            }

            used = true;
            textureName = EmptyTexture;
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

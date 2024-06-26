using Microsoft.Xna.Framework.Graphics;
using StardewValley;
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

        public override Rectangle getBoundingBox(Vector2 tileLocation)
        {
            return new((int)tileLocation.X * 64, (int)(tileLocation.Y + 1) * 64, 192, 128);
        }

        public override bool performUseAction(Vector2 tileLocation, GameLocation location)
        {
            if (used || !Curse.HasAnyCurse())
                return false;

            used = true;
            textureName += "Empty";
            ResetTexture();
            Game1.playSound("glug");
            Curse.RemoveRandomCurse();

            Perks.PerkType? randomPerk = Perks.GetRandomUniquePerk();
            if (randomPerk.HasValue)
                Perks.AddPerk(randomPerk.Value);

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

        public override void draw(SpriteBatch spriteBatch, Vector2 tileLocation)
        {
            spriteBatch.Draw(texture.Value, Game1.GlobalToLocal(Game1.viewport, tileLocation * 64f), new Rectangle(0, 0, 48, 48), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (tileLocation.Y + 2f) * 64f / 10000f);
        }
    }
}

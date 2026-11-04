using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley;
using StardewValley.Buffs;
using StardewValley.TerrainFeatures;
using System;

namespace StardewRoguelike.TerrainFeatures
{
    internal class SpeedPad : TerrainFeature
    {
        private string textureName = "TerrainFeatures\\SpeedPad";

        private Lazy<Texture2D> texture = null!;

        private Rectangle SourceRect = new(0, 0, 32, 16);

        private static readonly int FrameDuration = 200;

        private const string SpeedBuffId = $"{ModEntry.ModId}_SpeedPad";

        private const string SlowBuffId = $"{ModEntry.ModId}_SlowPad";

        private NetBool IsSlowPad { get; } = new();

        private int currentFrame = 0;

        private double frameCounter = 0;

        public SpeedPad() : base(true)
        {
            ResetTexture();
            NetFields.AddField(IsSlowPad);
        }

        public SpeedPad(bool isSlowPad) : this()
        {
            IsSlowPad.Value = isSlowPad;
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
            return new Rectangle((int)Tile.X * 64, (int)Tile.Y * 64, 64 * 2, 64);
        }

        public override void doCollisionAction(Rectangle positionOfCollider, int speedOfCollision, Vector2 tileLocation, Character who)
        {
            if (who != Game1.player)
                return;

            string buffId = IsSlowPad.Value ? SlowBuffId : SpeedBuffId;
            int speedAdjustment = IsSlowPad.Value ? -3 : 3;
            string soundEffect = IsSlowPad.Value ? "debuffHit" : "cowboy_powerup";
            int duration = IsSlowPad.Value ? 750 : 1500;

            // Refresh in place while the player stays on the pad; re-applying would
            // rebuild the buff display on every collision tick.
            if (Game1.player.buffs.AppliedBuffs.TryGetValue(buffId, out Buff? existing))
            {
                existing.millisecondsDuration = duration;
                return;
            }

            Game1.playSound(soundEffect);
            Game1.player.applyBuff(new Buff(
                id: buffId,
                source: "Roguelike",
                displaySource: "Speedpad",
                duration: duration,
                effects: new BuffEffects() { Speed = { speedAdjustment } },
                isDebuff: IsSlowPad.Value
            ));
        }

        public override bool isPassable(Character? c = null)
        {
            return true;
        }

        public override bool tickUpdate(GameTime time)
        {
            if (IsSlowPad.Value && SourceRect.X != 32)
                SourceRect.X = 32;

            if (frameCounter >= FrameDuration)
            {
                currentFrame++;
                currentFrame %= 3;
                frameCounter = 0;

                SourceRect.Y = currentFrame * 16;
            }
            else
                frameCounter += time.ElapsedGameTime.TotalMilliseconds;

            return false;
        }

        public override void draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(texture.Value, Game1.GlobalToLocal(Game1.viewport, Tile * 64f), SourceRect, Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 64f / 10000f);
        }
    }
}

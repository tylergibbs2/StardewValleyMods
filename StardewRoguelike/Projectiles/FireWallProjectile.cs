using Microsoft.Xna.Framework;
using Netcode;
using StardewValley;
using StardewValley.Projectiles;

namespace StardewRoguelike.Projectiles
{
    public class FireWallProjectile : BasicProjectile
    {
        private NetFloat DistanceBeforeStopping = new();

        private NetFloat DisappearAfter = new();

        public FireWallProjectile() : base()
        {
        }

        public FireWallProjectile(float distanceBeforeStopping, float disappearAfter, int damageToFarmer, int parentSheetIndex, int bouncesTillDestruct, int tailLength, float rotationVelocity, float velocityX, float velocityY, Vector2 startingPosition, string collisionSound, string firingSound, bool explode, bool damagesMonsters = false, GameLocation? location = null, Character? firer = null, onCollisionBehavior? collisionBehavior = null)
            : base(damageToFarmer, parentSheetIndex, bouncesTillDestruct, tailLength, rotationVelocity, velocityX, velocityY, startingPosition, collisionSound, null, firingSound, explode, damagesMonsters, location, firer, collisionBehavior)
        {
            DisappearAfter.Value = disappearAfter;
            DistanceBeforeStopping.Value = distanceBeforeStopping;
        }

        protected override void InitNetFields()
        {
            base.InitNetFields();
            NetFields.AddField(DisappearAfter).AddField(DistanceBeforeStopping);
        }

        public override void behaviorOnCollisionWithPlayer(GameLocation location, Farmer player)
        {
            if (damagesMonsters.Value)
                return;

            if (debuff.Value is not null && player.CanBeDamaged() && Game1.random.Next(11) >= player.Immunity && !player.hasBuff(Buff.squidInkRavioli))
            {
                if (Game1.player == player)
                    player.applyBuff(debuff.Value);

                location.playSound(debuffSound.Value);
            }

            player.takeDamage(damageToFarmer.Value, overrideParry: true, null);
        }

        public override void updatePosition(GameTime time)
        {
            if (travelDistance >= DistanceBeforeStopping.Value)
                return;

            position.X += xVelocity.Value;
            position.Y += yVelocity.Value;
        }

        public override bool update(GameTime time, GameLocation location)
        {
            if (Game1.IsMasterGame)
            {
                DisappearAfter.Value -= (float)time.ElapsedGameTime.TotalSeconds;
                if (DisappearAfter.Value <= 0f)
                    location.projectiles.Remove(this);
            }

            if (base.update(time, location))
            {
                bool collidesWithPlayer = false;

                foreach (Farmer player in location.farmers)
                {
                    if (player.GetBoundingBox().Intersects(getBoundingBox()))
                    {
                        collidesWithPlayer = true;
                        break;
                    }
                }

                if (!collidesWithPlayer)
                    return true;
            }

            return false;
        }
    }
}

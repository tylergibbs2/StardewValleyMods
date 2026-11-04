using Microsoft.Xna.Framework;
using Netcode;
using StardewRoguelike.Bosses;
using StardewValley;
using StardewValley.Projectiles;

namespace StardewRoguelike.Projectiles
{
    public class ReturningProjectile : BasicProjectile
    {
        private readonly NetVector2 StartingPosition = new();

        private readonly NetFloat ReturningVelocityMultiplier = new();

        public ReturningProjectile() : base()
        {
        }

        public ReturningProjectile(float returningVelocityMultiplier, int damageToFarmer, int parentSheetIndex, int bouncesTillDestruct, int tailLength, float rotationVelocity, float velocityX, float velocityY, Vector2 startingPosition, string collisionSound, string firingSound, bool explode, bool damagesMonsters = false, GameLocation? location = null, Character? firer = null, onCollisionBehavior? collisionBehavior = null)
            : base(damageToFarmer, parentSheetIndex, bouncesTillDestruct, tailLength, rotationVelocity, velocityX, velocityY, startingPosition, collisionSound, null, firingSound, explode, damagesMonsters, location, firer, collisionBehavior)
        {
            ReturningVelocityMultiplier.Value = returningVelocityMultiplier;
            StartingPosition.Value = startingPosition;
        }

        protected override void InitNetFields()
        {
            base.InitNetFields();
            NetFields.AddField(ReturningVelocityMultiplier).AddField(StartingPosition);
        }

        public override void behaviorOnCollisionWithOther(GameLocation location)
        {
            damagesMonsters.Value = true;

            Vector2 newVelocity = StartingPosition.Value - position.Value;
            newVelocity.Normalize();
            newVelocity *= ReturningVelocityMultiplier.Value;

            rotation = RoguelikeUtility.VectorToRadians(newVelocity) + RoguelikeUtility.DegreesToRadians(90);

            xVelocity.Value = newVelocity.X;
            yVelocity.Value = newVelocity.Y;
        }
    }
}

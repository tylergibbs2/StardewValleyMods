using Microsoft.Xna.Framework;
using Netcode;
using StardewRoguelike.Bosses;
using StardewValley;
using StardewValley.Projectiles;
using System;

namespace StardewRoguelike.Projectiles
{
    public class CircularProjectile : BasicProjectile
    {
        private readonly NetFloat angle = new();

        private readonly NetFloat AngularVelocity = new();

        private readonly NetFloat Distance = new();

        private readonly NetVector2 PointOfRotation = new();

        public CircularProjectile() : base()
        {
        }

        public CircularProjectile(int damageToFarmer, int parentSheetIndex, int bouncesTillDestruct, int tailLength, float rotationVelocity, float angularVelocity, Vector2 pointOfRotation, Vector2 startingPosition, string collisionSound, string firingSound, bool explode, bool damagesMonsters = false, GameLocation? location = null, Character? firer = null, onCollisionBehavior? collisionBehavior = null)
            : base(damageToFarmer, parentSheetIndex, bouncesTillDestruct, tailLength, rotationVelocity, 0f, 0f, startingPosition, collisionSound, null, firingSound, explode, damagesMonsters, location, firer, collisionBehavior)
        {
            AngularVelocity.Value = angularVelocity;
            Distance.Value = Vector2.Distance(pointOfRotation, startingPosition);
            PointOfRotation.Value = pointOfRotation;

            angle.Value = RoguelikeUtility.VectorToRadians(pointOfRotation - startingPosition);
        }

        protected override void InitNetFields()
        {
            base.InitNetFields();
            NetFields.AddField(angle).AddField(AngularVelocity).AddField(Distance).AddField(PointOfRotation);
        }

        public override void updatePosition(GameTime time)
        {
            angle.Value += (float)time.ElapsedGameTime.TotalSeconds * AngularVelocity.Value;

            Vector2 displacement = new(Distance.Value * (float)Math.Sin(angle.Value), Distance.Value * (float)Math.Cos(angle.Value));
            position.X = PointOfRotation.Value.X + displacement.X;
            position.Y = PointOfRotation.Value.Y + displacement.Y;
        }
    }
}

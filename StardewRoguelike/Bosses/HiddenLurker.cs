using Microsoft.Xna.Framework;
using StardewRoguelike.Extensions;
using StardewRoguelike.Projectiles;
using StardewRoguelike.VirtualProperties;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Projectiles;
using System;
using System.Collections.Generic;

namespace StardewRoguelike.Bosses
{
    internal class HiddenLurker : LavaLurk, IBossMonster
    {
        public string DisplayName => "Chatto the Eternal Lurker";

        public string MapPath => "custom-lavalurk";

        public string TextureName => "Characters/Monsters/Lava Lurk";

        public Vector2 SpawnLocation => new(22, 13);

        public List<string> MusicTracks => new() { "VolcanoMines1", "VolcanoMines2" };

        public bool InitializeWithHealthbar => true;

        public float Difficulty { get; set; }

        private enum AttackType
        {
            Barrage,
            FireWall,
            ExplodingRock,
            BigFireball
        }

        private readonly List<AttackType> AttackHistory = new();

        private AttackType CurrentAttack;

        private int FireWallAngle = 0;

        private float FireballTimer;

        private bool WaitingToDive = false;

        // max health percent, shot angle
        private readonly List<(float, int)> WhenToFireWall = new()
        {
            (0.145f * 6, 135),
            (0.145f * 4, 45),
            (0.145f * 2, 90)
        };

        public HiddenLurker() : base() { }

        public HiddenLurker(float difficulty) : base(Vector2.Zero)
        {
            setTileLocation(SpawnLocation);
            Difficulty = difficulty;

            Scale = 2f;

            moveTowardPlayerThreshold.Value = 12;
            Speed = 0;

            resilience.Value = 0;
        }

        protected override void initNetFields()
        {
            base.initNetFields();
        }

        public override bool TargetInRange()
        {
            if (targettedFarmer == null)
                return false;

            if (Math.Abs(targettedFarmer.Position.X - Position.X) <= (12 * 64f) && Math.Abs(targettedFarmer.Position.Y - Position.Y) <= (12 * 64f))
                return true;

            return false;
        }

        public override void behaviorAtGameTick(GameTime time)
        {
            if (targettedFarmer is null || targettedFarmer.currentLocation != currentLocation || targettedFarmer.get_FarmerIsSpectating().Value)
            {
                targettedFarmer = null;
                targettedFarmer = findPlayer();
            }

            if (stateTimer > 0f)
            {
                stateTimer -= (float)time.ElapsedGameTime.TotalSeconds;
                if (stateTimer <= 0f)
                    stateTimer = 0f;
            }

            if (currentState.Value == State.Submerged)
            {
                swimSpeed = this.AdjustRangeForHealth(2, 6);
                if (stateTimer == 0f)
                {
                    currentState.Value = State.Lurking;
                    stateTimer = 0.1f;
                }
            }
            else if (currentState.Value == State.Lurking)
            {
                swimSpeed = this.AdjustRangeForHealth(1, 4);
                if (stateTimer == 0f)
                {
                    currentState.Value = State.Emerged;
                    stateTimer = 0.75f;
                    swimSpeed = 0;
                }
            }
            else if (currentState.Value == State.Emerged)
            {
                if (stateTimer == 0f && !WaitingToDive && TargetInRange())
                {
                    AttackHistory.Add(CurrentAttack);

                    if (AttackHistory.Count >= 2)
                        AttackHistory.RemoveAt(0);

                    var validAttacks = new List<AttackType>((IEnumerable<AttackType>)Enum.GetValues(typeof(AttackType)));
                    validAttacks.Remove(AttackType.FireWall);
                    validAttacks.RemoveAll(attackType => AttackHistory.Contains(attackType));

                    currentState.Value = State.Firing;
                    stateTimer = 1.5f;

                    CurrentAttack = validAttacks[Game1.random.Next(validAttacks.Count)];

                    if (WhenToFireWall.Count > 0 && Health <= (int)Math.Round(MaxHealth * WhenToFireWall[0].Item1))
                    {
                        FireWallAngle = WhenToFireWall[0].Item2;
                        WhenToFireWall.RemoveAt(0);
                        CurrentAttack = AttackType.FireWall;
                    }

                    if (CurrentAttack == AttackType.ExplodingRock || CurrentAttack == AttackType.Barrage)
                        stateTimer += this.AdjustRangeForHealth(0f, 2f);
                    else if (CurrentAttack == AttackType.BigFireball)
                        stateTimer = 5f;

                    fireTimer = 0.25f;
                }
                else if (stateTimer == 0f && WaitingToDive)
                {
                    currentState.Value = State.Diving;
                    stateTimer = 0.5f;
                    WaitingToDive = false;
                }
            }
            else if (currentState.Value == State.Firing)
            {
                if (stateTimer == 0f && FireballTimer == 0f)
                {
                    currentState.Value = State.Emerged;
                    stateTimer = 2.5f;
                    WaitingToDive = true;

                    if (Roguelike.HardMode)
                    {
                        stateTimer = 1f;
                        WaitingToDive = false;
                    }
                }

                if (FireballTimer > 0f)
                {
                    FireballTimer -= (float)time.ElapsedGameTime.TotalSeconds;
                    if (FireballTimer <= 0f)
                    {
                        Vector2 shot_origin = Position + new Vector2(0f, -32f);
                        Vector2 shot_velocity = targettedFarmer.Position - shot_origin;
                        shot_velocity.Normalize();
                        shot_velocity *= 7f + (Roguelike.HardMode ? 2f : 0);
                        currentLocation.playSound("fireball");

                        ReturningProjectile returningShot = new(10f, DamageToFarmer, 14, 0, 3, (float)Math.PI / 16f, shot_velocity.X, shot_velocity.Y, shot_origin, "", "", explode: false, damagesMonsters: false, currentLocation, this);
                        returningShot.ignoreTravelGracePeriod.Value = true;

                        currentLocation.projectiles.Add(returningShot);

                        FireballTimer = 0f;
                    }
                }

                if (fireTimer > 0f)
                {
                    fireTimer -= (float)time.ElapsedGameTime.TotalSeconds;
                    if (fireTimer <= 0f)
                    {
                        if (CurrentAttack == AttackType.Barrage)
                        {
                            fireTimer = 0.25f;
                            if (targettedFarmer != null)
                            {
                                Vector2 shot_origin = Position + new Vector2(0f, -32f);
                                Vector2 shot_velocity = targettedFarmer.Position - shot_origin;
                                shot_velocity.Normalize();
                                shot_velocity *= 7f + (Roguelike.HardMode ? 2f : 0);
                                currentLocation.playSound("fireball");

                                BasicProjectile projectile = new(DamageToFarmer, 10, 0, 3, (float)Math.PI / 16f, shot_velocity.X, shot_velocity.Y, shot_origin, "", "", explode: false, damagesMonsters: false, currentLocation, this);
                                projectile.ignoreMeleeAttacks.Value = true;
                                projectile.ignoreTravelGracePeriod.Value = true;
                                currentLocation.projectiles.Add(projectile);

                                if (stateTimer <= 0.25f)
                                {
                                    FireballTimer = 0.25f;
                                    fireTimer = 0f;
                                }
                            }
                        }
                        else if (CurrentAttack == AttackType.FireWall)
                        {
                            if (targettedFarmer != null)
                            {
                                Vector2 shot_origin = Position + new Vector2(0f, -32f);

                                Vector2 shot_velocity;
                                shot_velocity = RoguelikeUtility.VectorFromDegrees(FireWallAngle);
                                shot_velocity *= 7f;

                                currentLocation.playSound("furnace");

                                for (int i = 64; i <= 64 * 18; i += 32)
                                {
                                    FireWallProjectile projectile = new(i, 120f, DamageToFarmer * 2, 10, 0, 0, (float)Math.PI / 16f, shot_velocity.X, shot_velocity.Y, shot_origin, "", "", explode: false, damagesMonsters: false, currentLocation, this);
                                    projectile.ignoreMeleeAttacks.Value = true;
                                    projectile.ignoreTravelGracePeriod.Value = true;
                                    projectile.startingScale.Value = Roguelike.HardMode ? 3f : 1f;
                                    currentLocation.projectiles.Add(projectile);
                                }
                            }

                            if (Health < (int)(MaxHealth * 0.33f) && stateTimer > 0f)
                            {
                                fireTimer = 1f;
                                stateTimer = 0f;
                            }
                        }
                        else if (CurrentAttack == AttackType.ExplodingRock)
                        {
                            if (targettedFarmer != null)
                            {
                                int spreadX = Game1.random.Next(-5, 6);
                                int spreadY = Game1.random.Next(-5, 6);

                                Vector2 targetTile = new(
                                    targettedFarmer.getTileLocation().X + spreadX,
                                    targettedFarmer.getTileLocation().Y + spreadY
                                );

                                Vector2 shot_origin = Position + new Vector2(0f, -32f);
                                float speedMultiplier = Roguelike.HardMode ? 10f : 8f;
                                ExplodingRockProjectile projectile = new(targetTile, speedMultiplier, DamageToFarmer, shot_origin, "fireball", currentLocation, this);

                                projectile.maxTravelDistance.Value = -1;
                                projectile.ignoreMeleeAttacks.Value = true;
                                projectile.ignoreTravelGracePeriod.Value = true;

                                currentLocation.projectiles.Add(projectile);

                                fireTimer = 0.175f;

                                if (stateTimer <= 0.25f)
                                {
                                    FireballTimer = 0.25f;
                                    fireTimer = 0f;
                                }
                            }
                        }
                        else if (CurrentAttack == AttackType.BigFireball)
                        {
                            if (targettedFarmer != null)
                            {
                                Vector2 shot_origin = Position + new Vector2(0f, -32f);
                                Vector2 shot_velocity = targettedFarmer.Position - shot_origin;
                                shot_velocity.Normalize();
                                shot_velocity *= 6f + this.AdjustRangeForHealth(0f, 10f) + (Roguelike.HardMode ? 2f : 0);
                                currentLocation.playSound("fireball");

                                BasicProjectile projectile = new(DamageToFarmer, 10, 0, 0, (float)Math.PI / 16f, shot_velocity.X, shot_velocity.Y, shot_origin, "", "", explode: false, damagesMonsters: false, currentLocation, this);
                                projectile.ignoreMeleeAttacks.Value = true;
                                projectile.ignoreTravelGracePeriod.Value = true;
                                projectile.startingScale.Value = 2.5f;
                                currentLocation.projectiles.Add(projectile);

                                fireTimer = Roguelike.HardMode ? 0.5f : 1f;

                                if (stateTimer <= 1.1f)
                                {
                                    FireballTimer = 1f;
                                    fireTimer = 0f;
                                }
                            }
                        }
                    }
                }
            }
            else if (currentState.Value == State.Diving && stateTimer == 0f)
            {
                currentState.Value = State.Submerged;
                stateTimer = 0.75f;

                targettedFarmer = findPlayer();
            }
        }

        public override int takeDamage(int damage, int xTrajectory, int yTrajectory, bool isBomb, double addedPrecision, Farmer who)
        {
            int result = base.takeDamage(damage, xTrajectory, yTrajectory, isBomb, addedPrecision, who);
            if (Health <= 0)
                BossManager.Death(currentLocation, who, DisplayName);

            return result;
        }
    }
}

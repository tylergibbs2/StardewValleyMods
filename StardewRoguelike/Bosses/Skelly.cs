using HarmonyLib;
using StardewValley.Pathfinding;
using Microsoft.Xna.Framework;
using Netcode;
using StardewRoguelike.Extensions;
using StardewRoguelike.Projectiles;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Projectiles;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace StardewRoguelike.Bosses
{
    public class Skelly : Skeleton, IBossMonster
    {
        public string DisplayName => "Arc the Skeleton Mage";

        public string MapPath => "boss-skeleton";

        public string TextureName => "Characters\\Monsters\\Skeleton";

        public Vector2 SpawnLocation => new(16, 17);

        public List<string> MusicTracks
        {
            get
            {
                if (Roguelike.FloorTickCounter <= TicksToChangeMusic)
                    return new() { "gelus_defensor" };

                return new() { "gelus_defensor_no_intro" };
            }
        }

        public bool InitializeWithHealthbar => true;

        public float Difficulty { get; set; }

        private readonly List<Vector2> MageSpawnLocations = new()
        {
            new(16, 9),
            new(24, 15),
            new(24, 25),
            new(8, 25),
            new(8, 15)
        };

        private readonly List<CircularProjectile> ActiveBones = new();

        private bool SpottedPlayer;

        private readonly NetBool ThrowingAnim = new();

        private readonly NetBool Invincible = new();

        private readonly NetBool Charging = new();

        private bool SpawnedMages = false;
        private bool CachedMagesAreDead = false;

        private int TicksToAttack = 60 * 5;

        /// <summary>
        /// The boss's three attacks, cycled in order. The rotation is independent of
        /// the freeze AOE and of the mage phase, which run on their own timers.
        /// </summary>
        private enum Attack
        {
            Bones,
            FrostBolts,
            BoneCircles
        }

        /// <summary>The attack chosen last, and so the one the next pick follows.</summary>
        private Attack PreviousAttack = Attack.BoneCircles;

        /// <summary>
        /// The projectile attack currently being fired, if any.
        ///
        /// Bones and frost bolts used to own a counter pair each - BonesToThrow with
        /// NextBoneThrow, FrostBoltsToThrow with NextFrostBolt - which left "throwing
        /// bones and frost bolts at the same time" perfectly representable, and made
        /// the attack in progress something you worked out by seeing which counter
        /// happened to be non-zero. They are one pair and a name now.
        /// </summary>
        private readonly NetEnum<Attack> FiringAttack = new(Attack.Bones);

        private int ShotsLeft = 0;
        private int TicksToNextShot = 0;

        private int TicksToSpawnBoneCircles = 0;
        private int TicksToDespawnBoneCircle = 0;

        private int ChargingTicksLeft = 0;
        private int TicksToFreezeAOE = 11 * 60;

        private readonly double TicksToChangeMusic = (129_706 / 1000) * 60;

        public Skelly() : base(Vector2.Zero) { }

        public Skelly(float difficulty) : this()
        {
            setTileLocation(SpawnLocation);
            Difficulty = difficulty;

            Sprite.LoadTexture(TextureName);
            Scale = 3f;

            Speed++;

            moveTowardPlayerThreshold.Value = 16;
        }

        protected override void initNetFields()
        {
            base.initNetFields();
            NetFields.AddField(Invincible).AddField(Charging).AddField(ThrowingAnim).AddField(FiringAttack);
        }

        public override void update(GameTime time, GameLocation location)
        {
            if (Invincible.Value)
                startGlowing(Color.Orange, false, 0.1f);
            else if (Charging.Value)
                startGlowing(Color.Turquoise, false, 0.1f);

            if (!Invincible.Value && glowingColor == Color.Orange)
                stopGlowing();
            if (!Charging.Value && glowingColor == Color.Turquoise)
                stopGlowing();

            base.update(time, location);
        }

        public override void updateMovement(GameLocation location, GameTime time)
        {
            if (Charging.Value || ThrowingAnim.Value)
            {
                faceDirection(2);
                Halt();
            }
            else
                base.updateMovement(location, time);
        }

        protected override void updateMonsterSlaveAnimation(GameTime time)
        {
            base.updateMonsterSlaveAnimation(time);

            if (ThrowingAnim.Value)
            {
                if (invincibleCountdown > 0)
                {
                    invincibleCountdown -= time.ElapsedGameTime.Milliseconds;
                    if (invincibleCountdown <= 0)
                        stopGlowing();
                }

                Sprite.Animate(time, 20, 5, 150f);
                if (Sprite.currentFrame == 24)
                    Sprite.currentFrame = 23;
            }
        }

        public override void behaviorAtGameTick(GameTime time)
        {
            if (ThrowingAnim.Value && !Charging.Value)
            {
                if (invincibleCountdown > 0)
                {
                    invincibleCountdown -= time.ElapsedGameTime.Milliseconds;
                    if (invincibleCountdown <= 0)
                        stopGlowing();
                }

                Sprite.Animate(time, 20, 5, 150f);
                if (Sprite.currentFrame == 24)
                {
                    ThrowingAnim.Value = false;
                    Sprite.currentFrame = 23;
                }
            }

            if (Charging.Value)
            {
                if (ChargingTicksLeft > 0)
                {
                    ChargingTicksLeft--;

                    if (ChargingTicksLeft == 0)
                    {
                        FreezeAOE(SpawnedMages ? 5 : 3);
                        currentLocation.playSound("coldSpell");
                        Charging.Value = false;
                        TicksToFreezeAOE = SpawnedMages ? 8 * 60 : 14 * 60;
                    }
                }

                return;
            }

            if (TicksToFreezeAOE > 0 && !Invincible.Value)
            {
                TicksToFreezeAOE--;

                if (TicksToFreezeAOE == 0)
                {
                    ChargingTicksLeft = 80;
                    Charging.Value = true;
                }
            }

            if (!SpottedPlayer && Utility.doesPointHaveLineOfSightInMine(currentLocation, Tile, Player.Tile, 8))
            {
                controller = new PathFindController(this, currentLocation, new(Player.StandingPixel.X / 64, Player.StandingPixel.Y / 64), -1, null, 200);
                SpottedPlayer = true;
                if (controller is null || controller.pathToEndPoint is null || controller.pathToEndPoint.Count == 0)
                {
                    Halt();
                    facePlayer(Player);
                }
                currentLocation.playSound("skeletonStep");
                IsWalkingTowardPlayer = true;
            }

            if (TicksToDespawnBoneCircle > 0)
            {
                TicksToDespawnBoneCircle--;

                if (TicksToDespawnBoneCircle == 0)
                    DespawnBoneCircles();
            }

            if (TicksToSpawnBoneCircles > 0)
            {
                TicksToSpawnBoneCircles--;

                if (TicksToSpawnBoneCircles == 60)
                    RoguelikeUtility.DoAttackCue(currentLocation, 60);

                if (TicksToSpawnBoneCircles == 0)
                    SpawnBoneCircles();
            }

            if (ShotsLeft > 0)
            {
                if (TicksToNextShot == 0)
                {
                    if (FiringAttack.Value == Attack.Bones)
                        ThrowBone();
                    else
                        ThrowFrostBolt();

                    ShotsLeft--;
                    TicksToNextShot = 20;
                }
                else
                    TicksToNextShot--;
            }

            if (ThrowingAnim.Value && ShotsLeft == 0)
                ThrowingAnim.Value = false;

            if (TicksToAttack == 0 && TicksToSpawnBoneCircles == 0)
                TicksToAttack = SpawnedMages ? 60 * 4 : 60 * 5;

            if (TicksToAttack > 0)
            {
                TicksToAttack--;

                if (TicksToAttack == 0)
                {
                    Attack attack = PreviousAttack switch
                    {
                        Attack.Bones => Attack.FrostBolts,
                        Attack.FrostBolts => Attack.BoneCircles,
                        _ => Attack.Bones
                    };
                    PreviousAttack = attack;

                    if (attack == Attack.BoneCircles)
                        TicksToSpawnBoneCircles = 61;
                    else
                    {
                        ThrowingAnim.Value = true;
                        FiringAttack.Value = attack;

                        ShotsLeft = SpawnedMages ? 5 : 3;
                        if (Roguelike.HardMode)
                            ShotsLeft += 2;
                        TicksToNextShot = 20;
                    }
                }
            }

            if (SpawnedMages && MagesAreDead())
                Invincible.Value = false;

            if (Health <= MaxHealth * ((float)1 / 2) && !SpawnedMages)
            {
                SpawnMages();
                Invincible.Value = true;
            }
        }

        private void SpawnMages()
        {
            foreach (Vector2 tilePosition in MageSpawnLocations)
            {
                SkellyMinion mage = new(tilePosition * 64f, Difficulty, isMage: true);
                if (!Roguelike.HardMode)
                    mage.Speed = 0;
                currentLocation.characters.Add(mage);
            }

            Speed++;
            SpawnedMages = true;
        }

        private bool MagesAreDead()
        {
            // cached
            if (CachedMagesAreDead)
                return true;

            int count = 0;

            foreach (Character character in currentLocation.characters)
            {
                if (character is Skeleton && character != this)
                    count++;
            }

            if (count == 0)
            {
                CachedMagesAreDead = true;
                if (!Roguelike.HardMode)
                    Speed--;

                return true;
            }

            return false;
        }

        public void FreezeAOE(int radius)
        {
            Vector2 tileLocation = Tile;
            Vector2 currentTile = new(Math.Min(currentLocation.map.Layers[0].LayerWidth - 1, Math.Max(0f, tileLocation.X - radius)), Math.Min(currentLocation.map.Layers[0].LayerHeight - 1, Math.Max(0f, tileLocation.Y - radius)));

            Rectangle areaOfEffect = new((int)(tileLocation.X - radius) * 64, (int)(tileLocation.Y - radius) * 64, (radius * 2 + 1) * 64, (radius * 2 + 1) * 64);

            TemporaryAnimatedSpriteList sprites = new()
            {
                new TemporaryAnimatedSprite(23, 9999f, 6, 1, new Vector2(currentTile.X * 64f, currentTile.Y * 64f), flicker: false, (Game1.random.NextDouble() < 0.5))
                {
                    lightId = $"{ModEntry.ModId}_Skelly_{Game1.random.Next()}",
                    lightRadius = radius,
                    lightcolor = Color.Black,
                    alphaFade = 0.03f - radius * 0.003f,
                    Parent = currentLocation
                }
            };
            MethodInfo rumbleAndFade = currentLocation.GetType().BaseType!.GetMethod("rumbleAndFade", BindingFlags.Instance | BindingFlags.NonPublic)!;
            rumbleAndFade.Invoke(currentLocation, new object[] { 300 + radius * 100 });

            currentLocation.debuffPlayers(areaOfEffect, Buff.frozen);


            int startingI = (int)(tileLocation.X - radius) * 64;
            int startingJ = (int)(tileLocation.Y - radius) * 64;

            for (int i = startingI; i <= startingI + (radius * 2) * 64; i += 64)
            {
                for (int j = startingJ; j <= startingJ + (radius * 2) * 64; j += 64)
                {
                    sprites.Add(new(13, new Vector2(i, j), Color.White, 9, flipped: false, 50f)
                    {
                        delayBeforeAnimationStart = Game1.random.Next(200),
                        scale = Game1.random.Next(5, 15) / 10f
                    });
                }
            }

            RoguelikeUtility.MultiplayerInstance.broadcastSprites(currentLocation, sprites);
        }

        public void SpawnBoneCircles()
        {
            currentLocation.playSound("skeletonHit");

            foreach (Farmer farmer in currentLocation.farmers)
            {
                Vector2 playerCenter = new(farmer.Position.X, farmer.Position.Y - farmer.Sprite.getHeight() - 24);
                for (int xOffset = -64; xOffset <= 64; xOffset += 128)
                {
                    for (int yOffset = -64; yOffset <= 64; yOffset += 128)
                    {
                        CircularProjectile proj = new(
                            (int)(DamageToFarmer),
                            4,
                            0,
                            0,
                            xOffset < 0 ? 0.3f : -0.3f,
                            3f,
                            playerCenter,
                            new Vector2(playerCenter.X + xOffset, playerCenter.Y + yOffset),
                            "skeletonHit",
                            "",
                            false,
                            false,
                            currentLocation,
                            this,
                            null!
                        );
                        currentLocation.projectiles.Add(proj);
                        proj.IgnoreLocationCollision = true;
                        ActiveBones.Add(proj);
                    }
                }
            }

            TicksToDespawnBoneCircle = 100;
        }

        public void DespawnBoneCircles()
        {
            currentLocation.playSound("skeletonHit");

            foreach (CircularProjectile bone in ActiveBones)
                currentLocation.projectiles.Remove(bone);

            ActiveBones.Clear();
        }

        public void ThrowBone()
        {
            if (Player is null)
                return;

            float boneSpeed = SpawnedMages ? 10f : 8f;
            Vector2 v = Utility.getVelocityTowardPlayer(new Point((int)Position.X, (int)Position.Y), boneSpeed, Player);
            BasicProjectile projectile = new(DamageToFarmer, 4, 0, 0, (float)Math.PI / 16f, v.X, v.Y, new Vector2(Position.X, Position.Y), "skeletonHit", null, "skeletonStep", explode: false, damagesMonsters: false, currentLocation, this);
            projectile.IgnoreLocationCollision = true;
            projectile.ignoreMeleeAttacks.Value = true;
            currentLocation.projectiles.Add(projectile);
        }

        public void ThrowFrostBolt()
        {
            float boneSpeed = SpawnedMages ? 10f : 8f;
            Vector2 v = Utility.getVelocityTowardPlayer(new Point((int)Position.X, (int)Position.Y), boneSpeed, Player);
            BasicProjectile projectile = new(DamageToFarmer, 9, 0, 4, 0f, v.X, v.Y, new Vector2(Position.X, Position.Y), "flameSpellHit", null, "flameSpell", explode: false, damagesMonsters: false, currentLocation, this);
            projectile.IgnoreLocationCollision = true;
            projectile.ignoreMeleeAttacks.Value = true;
            currentLocation.projectiles.Add(projectile);
        }

        public override Rectangle GetBoundingBox()
        {
            int boxWidth = (int)(Sprite.SpriteWidth * 4 * 3 / 4 * Scale);
            int boxHeight = (int)(Sprite.SpriteHeight * Scale);
            return new Rectangle((int)Position.X - boxWidth / 3 + 8, (int)Position.Y + boxHeight / 2, boxWidth, boxHeight);
        }

        public override void shedChunks(int number)
        {
            Game1.createRadialDebris(currentLocation, Sprite.textureName.Value, new Rectangle(0, Sprite.SpriteHeight * 4, Sprite.SpriteWidth, Sprite.SpriteWidth), 8, GetBoundingBox().Center.X, GetBoundingBox().Center.Y, number, (int)Tile.Y, Color.White, 4f);
        }

        public override int takeDamage(int damage, int xTrajectory, int yTrajectory, bool isBomb, double addedPrecision, Farmer who)
        {
            int actualDamage = Math.Max(1, damage - resilience.Value);
            if (Invincible.Value || Charging.Value)
            {
                currentLocation.playSound("crafting");
                return 0;
            }
            if (Game1.random.NextDouble() < missChance.Value - missChance.Value * addedPrecision)
                actualDamage = -1;
            else
            {
                Health -= actualDamage;
                currentLocation.playSound("hitEnemy");
                setTrajectory(xTrajectory / 3, yTrajectory / 3);
                if (Health <= 0)
                {
                    deathAnimation();
                    BossManager.Death(currentLocation, who, DisplayName, SpawnLocation);
                }
            }

            return actualDamage;
        }
    }
}

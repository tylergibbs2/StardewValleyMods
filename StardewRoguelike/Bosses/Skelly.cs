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
using System.Linq;
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

        /// <summary>
        /// What the boss is doing, and only ever one of them.
        ///
        /// These used to be four timers running beside one another, with the freeze
        /// holding the rest of the tick back behind an early return rather than
        /// stopping it. A volley caught by the freeze was paused, not finished, so the
        /// shots it had left came out after the ice landed - the boss interrupting
        /// itself and then picking up where it left off, a second attack later.
        ///
        /// An attack now begins only from <see cref="Phase.Waiting"/>, so there is
        /// nowhere for two of them to overlap.
        /// </summary>
        private enum Phase
        {
            /// <summary>Counting down to the next attack.</summary>
            Waiting,

            /// <summary>Throwing bones or frost bolts, one shot at a time.</summary>
            Firing,

            /// <summary>Winding up the ring of bones.</summary>
            BoneCircles,

            /// <summary>Holding still and glowing, about to freeze the room.</summary>
            Freezing
        }

        private Phase CurrentPhase = Phase.Waiting;

        /// <summary>The freeze is ready and waiting for the boss to be free.</summary>
        private bool FreezeIsDue;

        /// <summary>Ticks between one shot of a volley and the next.</summary>
        private const int TicksBetweenShots = 20;

        /// <summary>How long the boss glows before the freeze lands.</summary>
        private const int ChargeTicks = 80;

        /// <summary>The telegraph before the ring of bones appears.</summary>
        private const int BoneCircleWindupTicks = 61;

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
            UpdateThrowAnimation(time);
            NoticePlayer();
            UpdateLingeringBoneCircles();

            // Both clocks run whatever the boss is doing, so the rhythm of the fight
            // is the same as it was and does not stretch by however long an attack
            // happens to take. What they no longer do is cut in: they come due, and
            // the boss acts on them the moment it is free.
            if (TicksToAttack > 0)
                TicksToAttack--;

            if (TicksToFreezeAOE > 0 && !Invincible.Value)
            {
                TicksToFreezeAOE--;

                if (TicksToFreezeAOE == 0)
                    FreezeIsDue = true;
            }

            switch (CurrentPhase)
            {
                case Phase.Firing:
                    TickFiring();
                    break;

                case Phase.BoneCircles:
                    TickBoneCircles();
                    break;

                case Phase.Freezing:
                    TickFreezing();
                    break;

                default:
                    TickWaiting();
                    break;
            }

            if (SpawnedMages && MagesAreDead())
                Invincible.Value = false;

            if (Health <= MaxHealth * ((float)1 / 2) && !SpawnedMages)
            {
                SpawnMages();
                Invincible.Value = true;
            }
        }

        /// <summary>Counting down to the next attack, and the only place one starts.</summary>
        private void TickWaiting()
        {
            // Taken here rather than wherever the timer happened to run out, which is
            // what let it land in the middle of a volley.
            if (FreezeIsDue)
            {
                FreezeIsDue = false;
                ChargingTicksLeft = ChargeTicks;
                Charging.Value = true;
                CurrentPhase = Phase.Freezing;
                return;
            }

            if (TicksToAttack == 0)
                BeginNextAttack();
        }

        private void BeginNextAttack()
        {
            // Reset as the attack is launched, not as it ends, which is how often the
            // boss attacked before any of this was untangled.
            TicksToAttack = SpawnedMages ? 60 * 4 : 60 * 5;

            Attack attack = PreviousAttack switch
            {
                Attack.Bones => Attack.FrostBolts,
                Attack.FrostBolts => Attack.BoneCircles,
                _ => Attack.Bones
            };
            PreviousAttack = attack;

            if (attack == Attack.BoneCircles)
            {
                TicksToSpawnBoneCircles = BoneCircleWindupTicks;
                CurrentPhase = Phase.BoneCircles;
                return;
            }

            ThrowingAnim.Value = true;
            FiringAttack.Value = attack;

            ShotsLeft = SpawnedMages ? 5 : 3;
            if (Roguelike.HardMode)
                ShotsLeft += 2;
            TicksToNextShot = TicksBetweenShots;

            CurrentPhase = Phase.Firing;
        }

        /// <summary>Firing a volley, one shot at a time, until it is spent.</summary>
        private void TickFiring()
        {
            if (TicksToNextShot > 0)
            {
                TicksToNextShot--;
                return;
            }

            if (FiringAttack.Value == Attack.Bones)
                ThrowBone();
            else
                ThrowFrostBolt();

            ShotsLeft--;
            TicksToNextShot = TicksBetweenShots;

            if (ShotsLeft == 0)
            {
                ThrowingAnim.Value = false;
                EnterWaiting();
            }
        }

        /// <summary>
        /// Winding up the ring of bones, up to the moment it lands.
        ///
        /// The phase ends when the bones appear, not when they go: they are left
        /// standing as a hazard while the boss gets on with something else, which is
        /// what the old timer did too.
        /// </summary>
        private void TickBoneCircles()
        {
            TicksToSpawnBoneCircles--;

            if (TicksToSpawnBoneCircles == 60)
                RoguelikeUtility.DoAttackCue(currentLocation, 60);

            if (TicksToSpawnBoneCircles == 0)
            {
                SpawnBoneCircles();
                EnterWaiting();
            }
        }

        /// <summary>Holding still and glowing, then letting the freeze go.</summary>
        private void TickFreezing()
        {
            ChargingTicksLeft--;

            if (ChargingTicksLeft > 0)
                return;

            FreezeAOE(SpawnedMages ? 5 : 3);
            currentLocation.playSound("coldSpell");
            Charging.Value = false;
            TicksToFreezeAOE = SpawnedMages ? 8 * 60 : 14 * 60;

            EnterWaiting();
        }

        private void EnterWaiting()
        {
            CurrentPhase = Phase.Waiting;
        }

        /// <summary>The ring of bones clears itself whatever the boss is doing.</summary>
        private void UpdateLingeringBoneCircles()
        {
            if (TicksToDespawnBoneCircle == 0)
                return;

            TicksToDespawnBoneCircle--;

            if (TicksToDespawnBoneCircle == 0)
                DespawnBoneCircles();
        }

        private void UpdateThrowAnimation(GameTime time)
        {
            if (!ThrowingAnim.Value || Charging.Value)
                return;

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

        private void NoticePlayer()
        {
            if (SpottedPlayer || !Utility.doesPointHaveLineOfSightInMine(currentLocation, Tile, Player.Tile, 8))
                return;

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

        /// <summary>How many mages a single player has to put down to carry on.</summary>
        /// <remarks>
        /// Three of the five spots, not all five. The boss is invincible until the
        /// last of them dies, so this is a gate rather than a fight, and five spread
        /// across the arena made most of it walking - at the eleventh floor, with
        /// whatever the second shop sold. One more per extra player, so a party does
        /// not clear the gate in the time it takes one of them to cross it.
        /// </remarks>
        private const int MagesForOnePlayer = 3;

        private void SpawnMages()
        {
            int count = Math.Min(
                MageSpawnLocations.Count,
                MagesForOnePlayer + Math.Max(0, Game1.getOnlineFarmers().Count - 1));

            foreach (Vector2 tilePosition in MageSpawnLocations.Take(count))
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

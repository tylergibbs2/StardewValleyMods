using Microsoft.Xna.Framework;
using Netcode;
using StardewValley;
using StardewValley.Monsters;
using StardewValley.Projectiles;
using System;
using System.Collections.Generic;
using StardewRoguelike.VirtualProperties;
using StardewValley.Locations;

namespace StardewRoguelike.Bosses
{
    public class MinionSpawnRectangle
    {
        public int X1;

        public int Y1;

        public int X2;

        public int Y2;

        public int Height;

        public int Width;

        public int Perimeter;

        public MinionSpawnRectangle(int x1, int y1, int x2, int y2)
        {
            X1 = x1;
            Y1 = y1;
            X2 = x2;
            Y2 = y2;

            Height = Y2 - Y1;
            Width = X2 - X1;

            Perimeter = (X2 - X1) * 2 + (Y2 - Y1) * 2;
        }

        public List<Vector2> GetRandomPositions(int amount)
        {
            List<Vector2> positions = new();

            // Each point is only taken once, so asking for more than the perimeter
            // holds would spin forever looking for one that is left. Callers ask for
            // twenty at most against a perimeter of eighty-eight, but the cost of
            // being wrong here is a frozen game rather than a wrong number.
            amount = Math.Min(amount, Perimeter);

            while (amount > 0)
            {
                Vector2 pos;

                int perimeterPoint = Game1.random.Next(Perimeter);
                if (perimeterPoint < Height)
                    pos = new(X1, Y1 + perimeterPoint);
                else if (perimeterPoint < Height + Width)
                    pos = new(X1 + perimeterPoint - Height, Y1 + Height);
                else if (perimeterPoint < (Height + Width) + Height)
                    pos = new(X1 + Width, Y1 + perimeterPoint - (Height + Width));
                else
                    pos = new(X1 + perimeterPoint - (Height + Width + Height), Y1);

                if (positions.Contains(pos))
                    continue;

                positions.Add(pos);
                amount--;
            }

            return positions;
        }
    }

    public class ShadowKing : Monster, IBossMonster
    {
        public string DisplayName => "Ozul the Shadow King";

        public string MapPath => "boss-shadowking";

        public string TextureName => "Characters\\Monsters\\Shadow Shaman_dangerous";

        public Vector2 SpawnLocation => new(21, 94);

        public List<string> MusicTracks
        {
            get
            {
                if (Encountered.Value)
                    return new() { "circus_freak" };

                return new() { "Lava_Ambient" };
            }
        }

        public float Difficulty { get; set; }

        public bool InitializeWithHealthbar => false;

        private bool MapChangesEnabled = false;

        /// <summary>
        /// What each blocked tile looked like before the seal overwrote it, so the
        /// teardown can put it back. An index of -1 means there was no tile there.
        /// </summary>
        private readonly List<(Vector2 Tile, int Index, string SheetId)> ReplacedTiles = new();

        /// <summary>The warps the seal added, so the teardown removes only those.</summary>
        private readonly List<Warp> AddedWarps = new();

        private readonly int[] TilesToBlockIds = new int[] { 104, 106, 103, 120, 122, 119 };

        private readonly List<Vector2> TilesToBlock = new()
        {
            new(20, 75),
            new(21, 75),
            new(22, 75),
            new(20, 76),
            new(21, 76),
            new(22, 76)
        };

        private readonly List<Vector2> TilesToWarp = new()
        {
            new(20, 70),
            new(21, 70),
            new(22, 70),
            new(20, 75),
            new(21, 75),
            new(22, 75),
            new(20, 76),
            new(21, 76),
            new(22, 76)
        };

        private readonly Vector2 WarpDestination = new(21, 78);

        private bool OldEncountered = false;

        public readonly NetBool Encountered = new();

        private readonly int ExplosionCooldownTicks = 300;

        private readonly int Width;
        private readonly int Height;

        private int AttackPhase = 0;

        private int TicksSinceLastExplosion = 0;
        private int TicksToExplode = 0;
        private int ExplosionRadius;
        private int ExplosionDamage;

        private int TicksToArrowVolley = 0;

        private int TicksToSpawnBadMinions = 0;
        private int TicksToJinxCircle = 0;

        private int TicksToSpawnGoodMinions = 0;

        public MinionSpawnRectangle Phase2_SpawnRect = new(10, 82, 32, 104);

        public ShadowKing() { }

        public ShadowKing(float difficulty) : base("Shadow Shaman", Vector2.Zero)
        {
            setTileLocation(SpawnLocation);
            Difficulty = difficulty;

            Width = 16;
            Height = 24;
            Sprite.SpriteWidth = Width;
            Sprite.SpriteHeight = Height;
            Sprite.LoadTexture(TextureName);
            Scale = 2f;

            moveTowardPlayerThreshold.Value = 16;
        }

        protected override void initNetFields()
        {
            base.initNetFields();
            NetFields.AddField(Encountered);

        }

        public override void reloadSprite(bool onlyAppearance = false)
        {
            Sprite = new(TextureName)
            {
                SpriteWidth = Width,
                SpriteHeight = Height
            };
            Sprite.LoadTexture(TextureName);
            HideShadow = true;
        }

        public override void update(GameTime time, GameLocation location)
        {
            base.update(time, location);

            if (Health > 0 && !MapChangesEnabled && Encountered.Value)
                EnableMapChanges();

            // The other half of the reconcile. Health is net-synced, so every client
            // that sealed its own copy of the arena through the branch above also
            // unseals it; only the machine that landed the killing blow runs
            // takeDamage, and the warps are local map state on each of them.
            if (Health <= 0 && MapChangesEnabled)
                DisableMapChanges();

            if (OldEncountered != Encountered.Value)
                RevealBoss();

            OldEncountered = Encountered.Value;
        }

        private static void RevealBoss()
        {
            BossManager.StartRenderHealthBar();
        }

        public override void behaviorAtGameTick(GameTime time)
        {
            base.behaviorAtGameTick(time);

            // Before the arena is sealed, not after. behaviorAtGameTick has no health
            // check of its own - Monster.update calls it whenever the monster is still
            // in the location's character list - and death clears MapChangesEnabled,
            // so sealing from below this guard would re-seal the arena around a dead
            // boss and add another nine warps every tick the player stood nearby.
            // Only vanilla removing the monster on the same frame it dies kept that
            // from happening, which is not a promise this method should rely on.
            if (Health <= 0)
                return;

            if (!MapChangesEnabled && withinPlayerThreshold(16))
                EnableMapChanges();

            // Explode on hit
            if (TicksToExplode > 0)
            {
                TicksToExplode--;

                if (TicksToExplode == 0)
                {
                    stopGlowing();
                    currentLocation.netAudio.StopPlaying("fuse");

                    currentLocation.playSound("explosion");
                    currentLocation.explode(new Vector2(Position.X / 64, Position.Y / 64), ExplosionRadius, Player, damageFarmers: true, damage_amount: (int)(ExplosionDamage * Difficulty));
                    TicksSinceLastExplosion = 0;
                }
            }
            else
            {
                TicksSinceLastExplosion++;
            }

            if (AttackPhase == 0)
            {
                // do phase 1 stuff

                if (withinPlayerThreshold(6) && TicksToExplode == 0 && TicksToArrowVolley == 0)
                    TicksToArrowVolley = Game1.random.Next(60 * 5, 60 * 7);

                if (TicksToArrowVolley > 0 && TicksToExplode == 0)
                {
                    TicksToArrowVolley--;

                    if (TicksToArrowVolley == 45)
                        RoguelikeUtility.DoAttackCue(currentLocation, 45);

                    if (TicksToArrowVolley == 0)
                        FireArrowCone();
                }

                // Phase 2 transition
                if (Health <= MaxHealth * 0.66f)
                {
                    currentLocation.netAudio.StopPlaying("fuse");

                    // Cancelling the fuse has to undo what starting it did. Without
                    // the glow being stopped the boss stays red for the rest of the
                    // fight, and without resetting this the explosion cooldown is
                    // still spent, so the next hit detonates another one instantly.
                    stopGlowing();
                    TicksSinceLastExplosion = 0;

                    TicksToExplode = 0;
                    TicksToArrowVolley = 0;

                    Halt();
                    currentLocation.playSound("explosion");
                    foreach (Farmer farmer in currentLocation.farmers)
                    {
                        Vector2 trajectory = Utility.getAwayFromPositionTrajectory(Game1.player.GetBoundingBox(), Position);
                        farmer.setTrajectory(trajectory * 2);
                    }

                    TicksToJinxCircle = 120;
                    TicksToSpawnBadMinions = 180;

                    AttackPhase = 1;
                }
            }
            else if (AttackPhase == 1)
            {
                // do phase 2 stuff

                if (TicksToSpawnBadMinions == 0)
                    TicksToSpawnBadMinions = 60 * Game1.random.Next(10, 13);

                if (TicksToJinxCircle == 0)
                    TicksToJinxCircle = 60 * Game1.random.Next(5, 9);

                if (TicksToSpawnBadMinions > 0)
                {
                    TicksToSpawnBadMinions--;

                    if (TicksToSpawnBadMinions == 0)
                    {
                        if (Roguelike.HardMode)
                            SpawnGoodMinions(7);
                        else
                            SpawnBadMinions(20);
                    }
                }

                if (TicksToJinxCircle > 0)
                {
                    TicksToJinxCircle--;

                    if (TicksToJinxCircle == 0)
                        FireJinxCircle();
                }

                if (Health <= MaxHealth * 0.25f)
                {
                    TicksToSpawnGoodMinions = 120;

                    speed = 4;
                    AttackPhase = 2;
                }
            }
            else if (AttackPhase == 2)
            {
                // do phase 3 stuff

                if (TicksToSpawnGoodMinions == 0)
                    TicksToSpawnGoodMinions = 60 * Game1.random.Next(8, 11);

                if (TicksToJinxCircle == 0)
                    TicksToJinxCircle = 60 * Game1.random.Next(3, 7);

                if (withinPlayerThreshold(6) && TicksToArrowVolley == 0)
                    TicksToArrowVolley = Game1.random.Next(60 * 5, 60 * 7);

                if (TicksToJinxCircle > 0)
                {
                    TicksToJinxCircle--;

                    if (TicksToJinxCircle == 0)
                        FireJinxCircle();
                }

                if (TicksToArrowVolley > 0)
                {
                    TicksToArrowVolley--;

                    if (TicksToArrowVolley == 45)
                        RoguelikeUtility.DoAttackCue(currentLocation, 45);

                    if (TicksToArrowVolley == 0)
                    {
                        speed = 4;
                        FireArrowCone();
                    }
                }

                if (TicksToSpawnGoodMinions > 0)
                {
                    TicksToSpawnGoodMinions--;

                    if (TicksToSpawnGoodMinions == 0)
                    {
                        if (Roguelike.HardMode)
                        {
                            SpawnGoodMinions(7);
                            SpawnBadMinions(10);
                        }
                        else
                            SpawnGoodMinions(3);
                        speed = 7;
                    }
                }
            }
        }

        private void StartExplosion()
        {
            if (AttackPhase == 0)
            {
                TicksToExplode = 120;
                ExplosionDamage = 25;
                ExplosionRadius = 3;
            }
            else if (AttackPhase == 1)
            {
                TicksToExplode = 90;
                ExplosionDamage = 40;
                ExplosionRadius = 6;
            }

            Halt();
            startGlowing(Color.Red, true, 0.5f);
            currentLocation.netAudio.StartPlaying("fuse");
        }

        private void SpawnBadMinions(int amount)
        {
            foreach (Vector2 pos in Phase2_SpawnRect.GetRandomPositions(amount))
            {
                ShadowKingMinion minion = new(pos, Difficulty);
                currentLocation.characters.Add(minion);
            }
        }

        private void SpawnGoodMinions(int amount)
        {
            foreach (Vector2 pos in Phase2_SpawnRect.GetRandomPositions(amount))
            {
                ShadowKingMinion minion = new(pos, Difficulty, true);
                currentLocation.characters.Add(minion);
            }
        }

        private void FireArrowCone()
        {
            if (Player is null)
                return;

            faceGeneralDirection(Player.Position, 0, false);

            Vector2 shot_origin = new(GetBoundingBox().X, GetBoundingBox().Y);
            float fire_angle = 0f;
            if (Roguelike.HardMode)
                fire_angle = RoguelikeUtility.VectorToDegrees(Player.Position - Position) - 90;
            else
            {
                switch (facingDirection.Value)
                {
                    case 0:
                        shot_origin.Y -= 64f;
                        fire_angle = 90f;
                        break;
                    case 1:
                        shot_origin.X += 64f;
                        fire_angle = 0f;
                        break;
                    case 2:
                        fire_angle = 270f;
                        break;
                    case 3:
                        shot_origin.X -= 64f;
                        fire_angle = 180f;
                        break;
                }
            }

            currentLocation.playSound("Cowboy_gunshot");
            for (int angleOffset = -45; angleOffset <= 45; angleOffset += 18)
            {
                Vector2 shot_velocity = new((float)Math.Cos(RoguelikeUtility.DegreesToRadians(fire_angle + angleOffset)), -(float)Math.Sin(RoguelikeUtility.DegreesToRadians(fire_angle + angleOffset)));
                shot_velocity *= 10f;
                BasicProjectile projectile = new((int)Math.Round(35 * Difficulty), 12, 0, 1, 0f, shot_velocity.X, shot_velocity.Y, shot_origin, "", null, "", false, false, currentLocation, this, null);
                projectile.startingRotation.Value = RoguelikeUtility.VectorToRadians(shot_velocity) + (float)RoguelikeUtility.DegreesToRadians(90);
                projectile.ignoreTravelGracePeriod.Value = true;
                projectile.ignoreMeleeAttacks.Value = true;
                currentLocation.projectiles.Add(projectile);
            }
        }

        private void FireJinxCircle()
        {
            if (Player is not null)
                faceGeneralDirection(Player.Position, 0, false);

            float fire_angle = 0f;
            for (int angleOffset = 0; angleOffset <= 360; angleOffset += 45)
            {
                Vector2 shot_velocity = new((float)Math.Cos(RoguelikeUtility.DegreesToRadians(fire_angle + angleOffset)), -(float)Math.Sin(RoguelikeUtility.DegreesToRadians(fire_angle + angleOffset)));
                shot_velocity *= 10f;
                DebuffingProjectile projectile = new(Buff.evilEye, 7, 1, 4, (float)Math.PI / 16f, shot_velocity.X, shot_velocity.Y, new Vector2(GetBoundingBox().X, GetBoundingBox().Y), currentLocation, this);
                projectile.maxTravelDistance.Value = 64 * 5;
                projectile.ignoreTravelGracePeriod.Value = true;
                currentLocation.projectiles.Add(projectile);
            }
        }

        private bool ShouldBeStill()
        {
            return (
                TicksToExplode > 0
            );
        }

        public override void MovePosition(GameTime time, xTile.Dimensions.Rectangle viewport, GameLocation currentLocation)
        {
            if (ShouldBeStill())
                return;

            base.MovePosition(time, viewport, currentLocation);
        }

        public override Rectangle GetBoundingBox()
        {
            int boxWidth = (int)(Sprite.SpriteWidth * 4 * 5 / 6 * Scale);
            int boxHeight = (int)(Sprite.SpriteHeight * Scale);
            return new Rectangle((int)Position.X - boxWidth / 4, (int)Position.Y + boxHeight / 2, boxWidth, boxHeight);
        }

        public void EnableMapChanges()
        {
            Encountered.Value = true;
            currentLocation.playSound("boulderBreak");

            ReplacedTiles.Clear();
            for (int i = 0; i < TilesToBlock.Count; i++)
            {
                int tileId = TilesToBlockIds[i];
                Vector2 tile = TilesToBlock[i];

                ReplacedTiles.Add((
                    tile,
                    currentLocation.getTileIndexAt((int)tile.X, (int)tile.Y, "Buildings"),
                    currentLocation.getTileSheetIDAt((int)tile.X, (int)tile.Y, "Buildings")
                ));

                currentLocation.setMapTile((int)tile.X, (int)tile.Y, tileId, "Buildings", currentLocation.map.TileSheets[0].Id);
            }

            AddedWarps.Clear();
            foreach (Vector2 warpTile in TilesToWarp)
            {
                Warp warp = new((int)warpTile.X, (int)warpTile.Y, currentLocation.Name, (int)WarpDestination.X, (int)WarpDestination.Y, flipFarmer: false);
                AddedWarps.Add(warp);
                currentLocation.warps.Add(warp);
            }

            ((MineShaft)currentLocation).get_MineShaftCustomDestination().Value = WarpDestination;

            MapChangesEnabled = true;
        }

        /// <summary>
        /// Undoes <see cref="EnableMapChanges"/>. Safe to call on any machine and
        /// safe to call twice; it only touches what the seal itself added.
        ///
        /// This used to flip the flag and create the exit ladder without removing
        /// anything, so the warp traps on the entrance corridor and the blocked
        /// tiles outlived the fight and kept teleporting players back into a dead
        /// boss's arena for as long as the level stayed loaded.
        /// </summary>
        public void DisableMapChanges()
        {
            if (!MapChangesEnabled)
                return;

            foreach (Warp warp in AddedWarps)
                currentLocation.warps.Remove(warp);
            AddedWarps.Clear();

            foreach ((Vector2 tile, int index, string sheetId) in ReplacedTiles)
            {
                if (index < 0)
                    currentLocation.removeMapTile((int)tile.X, (int)tile.Y, "Buildings");
                else
                    currentLocation.setMapTile((int)tile.X, (int)tile.Y, index, "Buildings", sheetId);
            }
            ReplacedTiles.Clear();

            ((MineShaft)currentLocation).get_MineShaftCustomDestination().Value = Vector2.Zero;

            MapChangesEnabled = false;
        }

        public override int takeDamage(int damage, int xTrajectory, int yTrajectory, bool isBomb, double addedPrecision, Farmer who)
        {
            if (isBomb)
                return 0;

            if ((AttackPhase == 0 || AttackPhase == 1) && TicksSinceLastExplosion > ExplosionCooldownTicks && TicksToExplode == 0)
                StartExplosion();

            int result = base.takeDamage(damage, xTrajectory, yTrajectory, isBomb, addedPrecision, who);
            if (Health <= 0)
            {
                DisableMapChanges();

                // The exit, as opposed to undoing the seal: this is the one part of
                // death that must happen once rather than once per machine, so it
                // stays here instead of in DisableMapChanges, which every client
                // reaches through its own update loop.
                ((MineShaft)currentLocation).createLadderAt(WarpDestination);

                BossManager.Death(who.currentLocation, who, DisplayName, SpawnLocation);
            }

            return result;
        }

        protected override void sharedDeathAnimation()
        {
            Game1.createRadialDebris(currentLocation, TextureName, new Rectangle(Sprite.SourceRect.X, Sprite.SourceRect.Y, 16, 5), 16, StandingPixel.X, StandingPixel.Y - 32, 1, StandingPixel.Y / 64, Color.White);
            Game1.createRadialDebris(currentLocation, TextureName, new Rectangle(Sprite.SourceRect.X + 2, Sprite.SourceRect.Y + 5, 16, 5), 10, StandingPixel.X, StandingPixel.Y - 32, 1, StandingPixel.Y / 64, Color.White);
            Game1.createRadialDebris(currentLocation, TextureName, new Rectangle(0, 10, 16, 5), 16, StandingPixel.X, StandingPixel.Y - 32, 1, StandingPixel.Y / 64, Color.White);
        }

        protected override void localDeathAnimation()
        {
            Utility.makeTemporarySpriteJuicier(new TemporaryAnimatedSprite(45, base.Position, Color.White, 10), base.currentLocation);
            for (int i = 1; i < 3; i++)
            {
                base.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(6, base.Position + new Vector2(1f, 1f) * 64f * i, Color.Gray * 0.75f, 10)
                {
                    delayBeforeAnimationStart = i * 159
                });
                base.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(6, base.Position + new Vector2(1f, -1f) * 64f * i, Color.Gray * 0.75f, 10)
                {
                    delayBeforeAnimationStart = i * 159
                });
                base.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(6, base.Position + new Vector2(-1f, 1f) * 64f * i, Color.Gray * 0.75f, 10)
                {
                    delayBeforeAnimationStart = i * 159
                });
                base.currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(6, base.Position + new Vector2(-1f, -1f) * 64f * i, Color.Gray * 0.75f, 10)
                {
                    delayBeforeAnimationStart = i * 159
                });
            }
        }

        protected override void updateMonsterSlaveAnimation(GameTime time)
        {
            if (this.isMoving())
            {
                if (this.FacingDirection == 0)
                {
                    this.Sprite.AnimateUp(time);
                }
                else if (this.FacingDirection == 3)
                {
                    this.Sprite.AnimateLeft(time);
                }
                else if (this.FacingDirection == 1)
                {
                    this.Sprite.AnimateRight(time);
                }
                else if (this.FacingDirection == 2)
                {
                    this.Sprite.AnimateDown(time);
                }
            }
            else
            {
                this.Sprite.StopAnimation();
            }
        }
    }
}

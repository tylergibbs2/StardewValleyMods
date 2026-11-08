using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewRoguelike.Extensions;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Monsters;
using StardewValley.Objects;
using StardewValley.Tools;
using System.Collections.Generic;

namespace StardewRoguelike.ChallengeFloors
{
    internal class SlingshotAim : ChallengeBase
    {
        /// <summary>
        /// The tiles targets live in, and the area the camera frames.
        ///
        /// Both halves of keeping the floor playable hang off this one rectangle: the
        /// camera is parked on its centre, and the targets are turned back at its
        /// edges. Widen it and the targets get harder to see; narrow it and they
        /// bunch up.
        /// </summary>
        internal static readonly Rectangle Arena = new(8, 12, 21, 5);

        /// <summary>
        /// How far inside the arena a target turns back.
        ///
        /// The arena is a tile wider than a frame centred on it can show, so a target
        /// walking right to the edge ends up half off the screen. It turns a tile
        /// early instead.
        /// </summary>
        internal const int PatrolMargin = 1;

        private readonly Vector2 upperLeftCorner = new(Arena.Left, Arena.Top);

        private readonly Vector2 bottomRightCorner = new(Arena.Right, Arena.Bottom);

        private readonly NetInt floorSecondsLeft = new(30);

        private readonly NetInt wavesKilled = new(0);

        private int tickCounter = 0;

        private bool spawnedFirstWave = false;

        private bool shotProjectile = false;

        public override List<string> MapPaths => new() { "custom-slingshot" };

        protected override void InitNetFields()
        {
            base.InitNetFields();

            NetFields.AddField(floorSecondsLeft).AddField(wavesKilled);
        }

        public override bool ShouldSpawnLadder(MineShaft mine)
        {
            return false;
        }

        public void RenderHud(object? sender, RenderedHudEventArgs e)
        {
            string timeText = I18n.ChallengeFloor_Shared_TimeLeft(seconds: floorSecondsLeft.Value);
            Vector2 textSize = Game1.smallFont.MeasureString(timeText);

            Point timerDrawPos = new(100, 16);

            IClickableMenu.drawTextureBox(
                e.SpriteBatch,
                timerDrawPos.X - 15,
                timerDrawPos.Y - 12,
                (int)textSize.X + 33,
                (int)textSize.Y + 20,
                Color.White
            );

            Utility.drawTextWithShadow(
                e.SpriteBatch,
                timeText,
                Game1.smallFont,
                new Vector2(timerDrawPos.X, timerDrawPos.Y),
                Color.Black
            );

            string wavesText = I18n.ChallengeFloor_Shared_WavesKilled(amount: wavesKilled.Value);
            Vector2 wavesSize = Game1.smallFont.MeasureString(wavesText);

            Point wavesDrawPos = new(100, 40 + (int)textSize.Y);

            IClickableMenu.drawTextureBox(
                e.SpriteBatch,
                wavesDrawPos.X - 15,
                wavesDrawPos.Y - 12,
                (int)wavesSize.X + 33,
                (int)wavesSize.Y + 20,
                Color.White
            );

            Utility.drawTextWithShadow(
                e.SpriteBatch,
                wavesText,
                Game1.smallFont,
                new Vector2(wavesDrawPos.X, wavesDrawPos.Y),
                Color.Black
            );
        }

        public override void PlayerEntered(MineShaft mine)
        {
            base.PlayerEntered(mine);
            Game1.chatBox.addMessage(I18n.ChallengeFloor_SlingshotAim_WelcomeMessage(), Color.Gold);

            ModEntry.Events.Display.RenderedHud += RenderHud;

            Slingshot slingshot = new();
            slingshot.attachments[0] = new("388", 50);

            Vector2 chestSpot = new(6, 23);
            List<Item> chestItems = new()
            {
                slingshot
            };
            Chest chest = new(chestItems, chestSpot)
            {
                Tint = Color.White
            };
            mine.overlayObjects.Add(chestSpot, chest);
        }

        public override void PlayerLeft(MineShaft mine)
        {
            base.PlayerLeft(mine);
            Dispose();
        }

        public override void Dispose()
        {
            ModEntry.Events.Display.RenderedHud -= RenderHud;
            ReleaseCamera();
        }

        public void SpawnWave(MineShaft mine)
        {
            int monstersToSpawn = 6;
            if (Curse.AnyFarmerHasCurse(CurseType.MoreEnemiesLessHealth))
                monstersToSpawn++;

            monstersToSpawn *= Game1.getOnlineFarmers().Count;

            for (int i = 0; i < monstersToSpawn; i++)
                SpawnEnemy(mine);
        }

        public void SpawnEnemy(MineShaft mine)
        {
            Vector2 randomTile;
            do
            {
                randomTile = new(
                    Game1.random.Next((int)upperLeftCorner.X, (int)bottomRightCorner.X + 1),
                    Game1.random.Next((int)upperLeftCorner.Y, (int)bottomRightCorner.Y + 1)
                );
            } while (!mine.CanItemBePlacedHere(randomTile));

            SlingshotTargetMinion target = new(randomTile * 64f);

            DelayedAction.functionAfterDelay(() =>
            {
                mine.characters.Add(target);
            }, Game1.random.Next(0, 1000));
        }

        public void GameOver(MineShaft mine)
        {
            Finish();
            mine.characters.RemoveWhere(c => c is Monster);

            if (wavesKilled.Value == 0)
                return;

            mine.playSound("discoverMineral");

            // find adjacent free tile to spawn the chest
            Vector2 chestSpot = new(28, 24);

            Color tint = Color.White;

            int gemReward = wavesKilled.Value switch
            {
                1 => 68,
                2 => 68,
                3 => 70,
                4 => 64,
                _ => 72,
            };

            int goldAmount = wavesKilled.Value switch
            {
                1 => 20,
                2 => 25,
                3 => 30,
                4 => 35,
                _ => 40
            };

            List<Item> chestItems = new()
            {
                new SObject(gemReward.ToString(), 1),
                new SObject("384", goldAmount)
            };

            mine.SpawnLocalChest(new(28, 24), chestItems);
        }

        /// <summary>
        /// Decides whether the camera belongs on the arena or on the player.
        ///
        /// It used to be parked on the arena from the moment the floor loaded, which
        /// left the player coming down the ladder somewhere off the bottom of a frame
        /// they could not move - unable to see themselves, or the chest holding the
        /// slingshot they had come for. It also stayed parked once the fight was won,
        /// hiding the reward.
        ///
        /// So it is only taken while there is something to aim with and something to
        /// aim at. Walking in and clearing up afterwards are the player's own.
        /// </summary>
        private void UpdateCamera()
        {
            if (IsOver || !HoldingSlingshot())
            {
                ReleaseCamera();
                return;
            }

            FrameArena();
        }

        private static bool HoldingSlingshot()
        {
            foreach (Item item in Game1.player.Items)
            {
                if (item is Slingshot)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Parks the camera so the whole shooting field is on screen.
        ///
        /// Re-applied every tick rather than set once: warping, a death or anything
        /// else that moves the player will put the camera back on them, and the point
        /// of this floor is that the targets are all visible at once rather than
        /// found by walking around after them.
        ///
        /// Centred on the foot of the arena rather than its middle, so that the band
        /// of targets sits in the upper half of the screen and the ground the player
        /// shoots from sits in the lower. Centring on the middle put the targets in
        /// the centre of the frame and the player below its edge.
        /// </summary>
        private static void FrameArena()
        {
            Game1.viewportFreeze = true;
            Game1.viewport.Location = new xTile.Dimensions.Location(
                Arena.Center.X * 64 - Game1.viewport.Width / 2,
                Arena.Bottom * 64 - Game1.viewport.Height / 2);
        }

        /// <summary>Hands the camera back to the player.</summary>
        private static void ReleaseCamera()
        {
            Game1.viewportFreeze = false;
        }

        public override void Update(MineShaft mine, GameTime time)
        {
            // Before the host gate: the camera is this machine's business, and the
            // host runs this for mines it is not standing in.
            if (Game1.currentLocation == mine)
                UpdateCamera();

            if (!Context.IsMainPlayer || !Game1.shouldTimePass() || IsOver)
                return;

            if (!spawnedFirstWave)
            {
                SpawnWave(mine);
                spawnedFirstWave = true;
            }

            if (!shotProjectile)
            {
                if (mine.projectiles.Count > 0)
                    shotProjectile = true;

                return;
            }

            tickCounter++;
            if (tickCounter >= 60)
            {
                if (mine.EnemyCount == 0)
                {
                    wavesKilled.Value++;
                    SpawnWave(mine);
                    mine.playSound("hoeHit");
                }
                else if (floorSecondsLeft.Value == 0)
                    GameOver(mine);
                else
                    floorSecondsLeft.Value--;

                tickCounter = 0;
            }
        }
    }

    internal class SlingshotTargetMinion : Monster
    {
        private static readonly int initialMovingTicks = 120;

        private readonly NetBool seenPlayer = new();

        private bool isMovingRight;

        private int movingTicksLeft = initialMovingTicks;

        private bool fullySpawned = false;

        public SlingshotTargetMinion()
        {
        }

        public SlingshotTargetMinion(Vector2 position)
            : base("Wilderness Golem", position)
        {
            isMovingRight = Game1.random.Next() < 0.5;
            IsWalkingTowardPlayer = false;
            Slipperiness = 3;
            HideShadow = true;
            jitteriness.Value = 0.0;

            Sprite.currentFrame = 16;
            Sprite.loop = false;
            Sprite.UpdateSourceRect();

            Speed = 1;
            Health = 1;
            MaxHealth = 1;
            resilience.Value = 0;
        }


        protected override void initNetFields()
        {
            base.initNetFields();

            NetFields.AddField(seenPlayer);
            position.Field.AxisAlignedMovement = true;
        }

        protected override void localDeathAnimation()
        {
            currentLocation.temporarySprites.Add(new TemporaryAnimatedSprite(46, Position, Color.DarkGray, 10));
            currentLocation.localSound("rockGolemDie");
        }

        protected override void sharedDeathAnimation()
        {
            Game1.createRadialDebris(currentLocation, Sprite.textureName.Value, new Rectangle(0, 576, 64, 64), 32, GetBoundingBox().Center.X, GetBoundingBox().Center.Y, Game1.random.Next(4, 9), (int)Tile.Y);
        }

        public override void noMovementProgressNearPlayerBehavior()
        {
        }

        public override void behaviorAtGameTick(GameTime time)
        {
            if (!seenPlayer.Value)
            {
                if (withinPlayerThreshold(64))
                {
                    currentLocation.playSound("rockGolemSpawn");
                    seenPlayer.Value = true;
                }
                else
                {
                    Sprite.currentFrame = 16;
                    Sprite.loop = false;
                    Sprite.UpdateSourceRect();
                }
            }
            else if (Sprite.currentFrame >= 16)
            {
                Sprite.Animate(time, 16, 8, 75f);
                if (Sprite.currentFrame >= 24)
                {
                    Sprite.loop = true;
                    moveTowardPlayerThreshold.Value = 1;
                    IsWalkingTowardPlayer = true;
                    HideShadow = false;
                    fullySpawned = true;
                }
            }

            if (seenPlayer.Value && fullySpawned)
            {
                if (movingTicksLeft > 0)
                {
                    movingTicksLeft--;

                    if (movingTicksLeft == 0)
                    {
                        isMovingRight = !isMovingRight;
                        movingTicksLeft = initialMovingTicks;
                    }
                }

                // Turn at the edges as well as on the timer. Without this the patrol
                // walks targets out of the framed arena and they cannot be seen, let
                // alone shot.
                if (Position.X <= (SlingshotAim.Arena.Left + SlingshotAim.PatrolMargin) * 64f)
                    isMovingRight = true;
                else if (Position.X >= (SlingshotAim.Arena.Right - SlingshotAim.PatrolMargin) * 64f)
                    isMovingRight = false;

                if (isMovingRight)
                {
                    moveRight = true;
                    moveLeft = false;
                    moveDown = false;
                    moveUp = false;
                }
                else
                {
                    moveRight = false;
                    moveLeft = true;
                    moveDown = false;
                    moveUp = false;
                }
            }
        }

        protected override void updateMonsterSlaveAnimation(GameTime time)
        {
            if (IsWalkingTowardPlayer)
            {
                if (FacingDirection == 0)
                    Sprite.AnimateUp(time);
                else if (FacingDirection == 3)
                    Sprite.AnimateLeft(time);
                else if (FacingDirection == 1)
                    Sprite.AnimateRight(time);
                else if (FacingDirection == 2)
                    Sprite.AnimateDown(time);
            }
            if (!seenPlayer.Value)
            {
                Sprite.currentFrame = 16;
                Sprite.loop = false;
                Sprite.UpdateSourceRect();
            }
            else if (Sprite.currentFrame >= 16)
            {
                Sprite.Animate(time, 16, 8, 75f);
                if (Sprite.currentFrame >= 24)
                {
                    Sprite.loop = true;
                    Sprite.currentFrame = 0;
                    Sprite.UpdateSourceRect();
                }
            }
        }
    }
}

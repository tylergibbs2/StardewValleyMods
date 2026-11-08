using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
using System;
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

        /// <summary>
        /// How far the player can stray from the middle of the frame before the
        /// camera starts following them.
        ///
        /// Three tiles, so shuffling about while lining up a shot does not slide the
        /// whole arena around, but walking down the floor does bring the far end into
        /// view.
        /// </summary>
        private const float DeadZone = 3 * 64f;

        /// <summary>
        /// How much of the distance to its mark the camera closes each tick.
        ///
        /// Low enough to read as a glide rather than a cut: about two thirds of the
        /// way there in a sixth of a second. It is also what makes taking the camera
        /// in the first place bearable, since it slides over from wherever the player
        /// was standing rather than jumping.
        /// </summary>
        private const float Smoothing = 0.12f;

        /// <summary>
        /// The row the player has to walk up to before the camera takes the floor.
        ///
        /// Below this they are fetching the slingshot and finding their feet, and the
        /// camera is theirs. Crossing it is stepping up to shoot, which is when being
        /// able to see the whole field starts to matter more than being able to see
        /// where you are standing.
        /// </summary>
        private const int FiringLineRow = 23;

        /// <summary>
        /// How far back over the line the player has to step before the camera is
        /// handed back, so that standing on it does not flicker.
        /// </summary>
        private const int FiringLineSlack = 1;

        /// <summary>How close counts as home when easing the camera back, in pixels.</summary>
        private const float HomeSettled = 8f;

        /// <summary>Tiles of air left around the arena when framing it.</summary>
        private const float ZoomMarginTiles = 2f;

        /// <summary>Tiles of ground below the arena the player shoots from.</summary>
        private const float ZoomGroundTiles = 6f;

        /// <summary>As far out as the floor will ever pull the camera.</summary>
        private const float MinimumZoom = 0.5f;

        /// <summary>Where the camera is now, in world pixels, before rounding.</summary>
        private float cameraX;
        private float cameraY;

        /// <summary>
        /// The player's own zoom, taken when the floor takes the camera and given
        /// back when it lets go.
        ///
        /// Written to desiredBaseZoomLevel rather than baseZoomLevel: Game1 re-asserts
        /// one from the other every tick, so setting the applied zoom put the two
        /// permanently at odds and the game reset it, forced a viewport snap and
        /// refreshed the window every frame, which stamped on the pan.
        ///
        /// Held rather than read each tick, because the field it is read from is the
        /// same one the floor writes to while it has the camera.
        /// </summary>
        private float restingZoom = 1f;

        /// <summary>
        /// Whether the player pins their toolbar, kept for the same reason.
        ///
        /// The toolbar takes itself to the top of the screen when the player is in
        /// the lower half of it, which on a framed arena is where the player always
        /// is - so it sat over the targets for the whole fight. Pinning is the base
        /// game's own answer to that, so the floor borrows it.
        /// </summary>
        private bool restingPinToolbar;

        /// <summary>Whether the camera is this floor's rather than the player's.</summary>
        private bool holdingCamera;

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
            bool wants = !IsOver && HoldingSlingshot() && AtFiringLine();

            // Nothing to do and nothing to undo.
            if (!wants && !holdingCamera)
                return;

            // The camera is picked up from wherever it already is, so the first frame
            // of holding it changes nothing and the glide does the moving.
            if (wants && !holdingCamera)
            {
                cameraX = Game1.viewport.X + Game1.viewport.Width / 2f;
                cameraY = Game1.viewport.Y + Game1.viewport.Height / 2f;
                restingZoom = Game1.options.desiredBaseZoomLevel;
                restingPinToolbar = Game1.options.pinToolbarToggle;
                Game1.options.pinToolbarToggle = true;

                // Set once rather than eased. Every change to the zoom has the game
                // rebuild both render targets and the lightmap, so easing it over
                // half a second rebuilt all three about twenty times - which is what
                // the flashing was, and the viewport snap each change forces was the
                // jitter. One change costs one rebuild, the same as the player
                // pressing the zoom button, and the pan still glides.
                Game1.options.desiredBaseZoomLevel = ArenaZoom();

                holdingCamera = true;
            }

            if (!wants)
            {
                // Eased home rather than dropped. Handing the camera straight back
                // snapped the view onto the player the instant they stepped over the
                // line, so the way out was animated and the way back was a cut. The
                // floor keeps hold until the view has arrived.
                Game1.viewportFreeze = true;

                float homeX = Game1.player.Position.X + 32f;
                float homeY = Game1.player.Position.Y + 32f;

                cameraX += (homeX - cameraX) * Smoothing;
                cameraY += (homeY - cameraY) * Smoothing;

                Game1.viewport.Location = new xTile.Dimensions.Location(
                    (int)(cameraX - Game1.viewport.Width / 2f),
                    (int)(cameraY - Game1.viewport.Height / 2f));

                // The zoom goes back last, inside ReleaseCamera, so the one rebuild
                // it costs lands on a camera that has already stopped moving.
                if (Math.Abs(homeX - cameraX) < HomeSettled
                    && Math.Abs(homeY - cameraY) < HomeSettled)
                {
                    ReleaseCamera();
                }

                return;
            }

            Game1.viewportFreeze = true;

            cameraX += (DesiredCentreX() - cameraX) * Smoothing;
            cameraY += (DesiredCentreY() - cameraY) * Smoothing;

            Game1.viewport.Location = new xTile.Dimensions.Location(
                (int)(cameraX - Game1.viewport.Width / 2f),
                (int)(cameraY - Game1.viewport.Height / 2f));
        }

        /// <summary>
        /// Whether the player has stepped up to shoot.
        ///
        /// The line sits a tile lower on the way back than on the way in, so standing
        /// on it does not hand the camera to and fro every other frame.
        /// </summary>
        private bool AtFiringLine()
        {
            Vector2 tile = Game1.player.Tile;

            // A tile of give once the camera is held, so standing on the line does
            // not hand it to and fro every other frame.
            int slack = holdingCamera ? FiringLineSlack : 0;

            // Across as well as up. The ladder in sits at (4, 22) - on the firing
            // line's row, but off to the west of the river - so walking in and out of
            // the floor took the camera with it. Standing in front of the targets
            // means being in front of them.
            if (tile.X < Arena.Left - slack || tile.X > Arena.Right + slack)
                return false;

            // Counted in tile rows rather than pixels. The water is rows eighteen to
            // twenty-one and the line sits just below it, so comparing raw positions
            // put a player standing on that very row on either side of it depending
            // on where in the tile they had stopped.
            return tile.Y <= FiringLineRow + slack;
        }

        /// <summary>
        /// How far out the camera has to pull for the arena and the ground below it
        /// to fit on the screen.
        ///
        /// Measured rather than picked, so it is right whatever size the window is
        /// and whatever the player has their own zoom set to. It will only ever pull
        /// out: a player who plays zoomed further out than this keeps their own view.
        /// </summary>
        private float ArenaZoom()
        {
            Viewport screen = Game1.graphics.GraphicsDevice.Viewport;

            float wanted = (Arena.Width + ZoomMarginTiles * 2) * 64f;
            float tall = (Arena.Height + ZoomGroundTiles + ZoomMarginTiles) * 64f;

            float fit = Math.Min(screen.Width / wanted, screen.Height / tall);

            return Math.Clamp(fit, MinimumZoom, restingZoom);
        }

        /// <summary>
        /// Where the camera wants to be across the floor.
        ///
        /// It follows the player, but only once they have pushed out of the middle of
        /// the frame, and never far enough to let an end of the arena off the screen.
        /// The targets are the thing that has to stay visible; the player is the thing
        /// that has to stay findable.
        /// </summary>
        private float DesiredCentreX()
        {
            float playerX = Game1.player.Position.X + 32f;
            float centre = cameraX;

            if (playerX > centre + DeadZone)
                centre = playerX - DeadZone;
            else if (playerX < centre - DeadZone)
                centre = playerX + DeadZone;

            float half = Game1.viewport.Width / 2f;
            float leftMost = Arena.Left * 64f + half;
            float rightMost = Arena.Right * 64f - half;

            // A frame wider than the arena has nowhere to pan to, so it sits on the
            // middle of it.
            if (leftMost > rightMost)
                return (Arena.Left + Arena.Right) * 32f;

            return Math.Clamp(centre, leftMost, rightMost);
        }

        /// <summary>
        /// And where it wants to be up and down, which is fixed.
        ///
        /// The band of targets is only a tile or so shorter than the frame, so there
        /// is no room to follow the player vertically without pushing the top of the
        /// band off the screen. It sits on the foot of the arena: targets in the upper
        /// half, the ground the player shoots from in the lower.
        /// </summary>
        private static float DesiredCentreY()
        {
            return Arena.Bottom * 64f;
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

        /// <summary>Hands the camera back to the player.</summary>
        /// <summary>
        /// Hands the camera and the zoom back, at once rather than over time.
        ///
        /// Called when the floor is done with them, and from Dispose - a player who
        /// has walked out of the floor must not be left looking at it from a distance.
        /// </summary>
        private void ReleaseCamera()
        {
            if (!holdingCamera)
                return;

            holdingCamera = false;
            Game1.viewportFreeze = false;

            Game1.options.desiredBaseZoomLevel = restingZoom;
            Game1.options.pinToolbarToggle = restingPinToolbar;
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

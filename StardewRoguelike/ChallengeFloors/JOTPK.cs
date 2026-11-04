using System;
using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewRoguelike.Extensions;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Monsters;
using System.Collections.Generic;

namespace StardewRoguelike.ChallengeFloors
{
    internal class JOTPK : ChallengeBase
    {
        private readonly NetBool gameOver = new(false);

        private readonly NetInt floorSecondsLeft = new(45);

        private bool spawnedFirstMonster = false;

        private bool killedFirstMonster = false;

        private int tickCounter = 0;

        private int ticksToSpawnEnemies = 0;

        private Rectangle spawnRectTop = new(18, 9, 4, 1);
        private Rectangle spawnRectRight = new(30, 18, 1, 4);
        private Rectangle spawnRectBottom = new(18, 31, 4, 1);
        private Rectangle spawnRectLeft = new(8, 18, 1, 4);

        public override List<string> MapPaths => new() { "custom-jotpk" };

        public override List<string> GetMusicTracks(MineShaft mine)
        {
            if (floorSecondsLeft.Value >= 45 || gameOver.Value)
                return new() { "none" };

            return new() { "Cowboy_OVERWORLD" };
        }

        public override Vector2? GetSpawnLocation(MineShaft mine)
        {
            return new(20, 9);
        }

        public JOTPK() : base() { }

        protected override void InitNetFields()
        {
            base.InitNetFields();

            NetFields.AddField(floorSecondsLeft);

            // Declared as a NetBool but never registered, so it stayed false on
            // farmhands: ShouldSpawnLadder answered no for them and the music never
            // changed. Only the host's explicit createLadderAt hid it.
            NetFields.AddField(gameOver);
        }

        public void RenderHud(object? sender, RenderedHudEventArgs e)
        {
            string timeText = $"Time Left: {floorSecondsLeft.Value}";
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
        }

        public override void PlayerEntered(MineShaft mine)
        {
            base.PlayerEntered(mine);
            mine.forceViewportPlayerFollow = false;

            Game1.chatBox.addMessage(I18n.ChallengeFloor_JOTPK_WelcomeMessage(), Color.Gold);

            ModEntry.Events.Display.RenderedHud += RenderHud;
        }

        public override void PlayerLeft(MineShaft mine)
        {
            base.PlayerLeft(mine);
            Dispose();
        }

        public override void Dispose()
        {
            ModEntry.Events.Display.RenderedHud -= RenderHud;
        }

        public override bool ShouldSpawnLadder(MineShaft mine) => gameOver.Value;

        public static Vector2 GetRandomTileInRect(Rectangle rect)
        {
            int x = Game1.random.Next(rect.X, rect.X + rect.Width + 1);
            int y = Game1.random.Next(rect.Y, rect.Y + rect.Height + 1);

            return new(x, y);
        }

        public void SpawnEnemies(MineShaft mine)
        {
            int monstersToSpawn = Game1.random.Next(1, 4);

            if (Curse.AnyFarmerHasCurse(CurseType.MoreEnemiesLessHealth))
                monstersToSpawn += 2;

            int sideToSpawn = Game1.random.Next(0, 4);
            Rectangle spawnRect = sideToSpawn switch
            {
                0 => spawnRectTop,
                1 => spawnRectRight,
                2 => spawnRectBottom,
                3 => spawnRectLeft,
                _ => throw new System.Exception("Invalid side to spawn"),
            };

            // Each side is four tiles and the loop below wants a distinct one per
            // monster, so asking for more than the side holds never finishes. The
            // MoreEnemiesLessHealth curse adds two to a roll of up to three, which
            // put it past four one time in three.
            monstersToSpawn = Math.Min(monstersToSpawn, spawnRect.Width * spawnRect.Height);

            List<Vector2> spawnTiles = new();
            while (spawnTiles.Count < monstersToSpawn)
            {
                Vector2 tile = GetRandomTileInRect(spawnRect);
                if (!spawnTiles.Contains(tile))
                    spawnTiles.Add(tile);
            }

            foreach (Vector2 tile in spawnTiles)
            {
                Monster monster = mine.BuffMonsterIfNecessary(mine.getMonsterForThisLevel(mine.mineLevel, (int)tile.X, (int)tile.Y));
                Roguelike.AdjustMonster(mine, ref monster);
                monster.moveTowardPlayerThreshold.Value = 50;

                mine.characters.Add(monster);
            }
        }

        public void GameOver(MineShaft mine)
        {
            mine.characters.RemoveWhere(c => c is Monster);

            gameOver.Value = true;
            mine.createLadderAt(new(20, 20));

            mine.playSound("discoverMineral");

            mine.SpawnLocalChest(new(20, 18));
        }

        public override void Update(MineShaft mine, GameTime time)
        {
            if (!Context.IsMainPlayer || !Game1.shouldTimePass() || gameOver.Value)
                return;

            if (!spawnedFirstMonster)
            {
                Monster monster = new Skeleton(new(20 * 64, 20 * 64));
                monster.Speed = 0;
                monster.DamageToFarmer = 0;
                monster.Health = 1;
                monster.MaxHealth = 1;
                mine.characters.Add(monster);

                spawnedFirstMonster = true;
            }
            else if (!killedFirstMonster)
            {
                if (mine.EnemyCount == 0)
                {
                    ticksToSpawnEnemies = 150;
                    killedFirstMonster = true;
                }

                return;
            }

            if (ticksToSpawnEnemies > 0 && floorSecondsLeft.Value > 0)
            {
                ticksToSpawnEnemies--;

                if (ticksToSpawnEnemies == 0)
                {
                    SpawnEnemies(mine);
                    ticksToSpawnEnemies = Game1.random.Next(2 * 60, 10 * 60);
                    for (int i = 0; i < Game1.getOnlineFarmers().Count; i++)
                    {
                        if (i == 0)
                            continue;

                        // Integer division, once per extra player. Reaching zero would
                // stop the spawner for good, since it only re-arms from its own
                // countdown.
                ticksToSpawnEnemies = Math.Max(1, ticksToSpawnEnemies / 2);
                    }
                }
            }

            tickCounter++;
            if (tickCounter >= 60)
            {
                if (floorSecondsLeft.Value > 0)
                    floorSecondsLeft.Value--;
                else if (mine.EnemyCount == 0)
                    GameOver(mine);

                tickCounter = 0;
            }
        }
    }
}

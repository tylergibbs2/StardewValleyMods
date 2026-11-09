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
    internal class Race : ChallengeBase
    {
        int tickCounter = 0;

        bool spawnedFirstWave = false;

        private readonly NetInt floorSecondsLeft = new(60);

        private readonly NetInt wavesKilled = new(0);

        public Race() : base() { }

        protected override void InitNetFields()
        {
            base.InitNetFields();

            NetFields.AddField(floorSecondsLeft).AddField(wavesKilled);
        }

        public override List<string> MapPaths => new() {
            "3", "21", "23", "custom-1",
            "custom-2", "custom-3", "custom-4",
            "custom-5", "custom-6", "custom-7"
        };

        public void GameOver(MineShaft mine)
        {
            Finish();
            mine.createLadderAt(ChallengeFloor.GetSpawnLocation(mine));

            mine.characters.RemoveWhere(c => c is Monster);

            if (wavesKilled.Value == 0)
                return;

            mine.playSound("discoverMineral");

            // find adjacent free tile to spawn the chest
            Vector2 spawnLocation = ChallengeFloor.GetSpawnLocation(mine);
            // Falls back to the spawn tile itself: left at zero the reward chest is
            // placed at the top-left corner of the map, where the player cannot
            // reach it, and the floor is won with nothing to show for it.
            Vector2 chestSpot = spawnLocation;
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0)
                        continue;

                    if (mine.CanItemBePlacedHere(new Vector2((int)spawnLocation.X + i, (int)spawnLocation.Y + j)))
                    {
                        chestSpot = new((int)spawnLocation.X + i, (int)spawnLocation.Y + j);
                        break;
                    }
                }
            }

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

            mine.SpawnLocalChest(chestSpot, chestItems);
        }

        public override bool ShouldSpawnLadder(MineShaft mine)
        {
            return IsOver;
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
            Game1.chatBox.addMessage(I18n.ChallengeFloor_Race_WelcomeMessage(), Color.Gold);

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

        /// <summary>How close a wave may arrive, and how far off it may start.</summary>
        /// <remarks>
        /// Near enough to walk in on their own. A wave used to be scattered over the
        /// whole of a full-sized mine floor, and since the next one does not come
        /// until the room is clear, the floor was four short fights joined by four
        /// long walks - with the clock running during the walks. Close enough to
        /// find, far enough not to land on top of anybody.
        /// </remarks>
        private const int NearestSpawn = 6;

        private const int FurthestSpawn = 16;

        public void SpawnWave(MineShaft mine)
        {
            int monstersToSpawn = 4;
            if (Curse.AnyFarmerHasCurse(CurseType.MoreEnemiesLessHealth))
                monstersToSpawn++;

            // A share around each player, not a pile around one of them. Update
            // runs on the host, so spawning near "the player" would have put every
            // wave of a co-op run on top of whoever was hosting and left everybody
            // else walking - which is the thing this floor is being fixed for.
            //
            // Chasers only. The wave has to arrive by itself for any of the above to
            // mean anything, and the mine is perfectly happy to hand back a rock
            // crab that will sit where it is put until the floor times out.
            foreach (Farmer farmer in Game1.getOnlineFarmers())
            {
                if (farmer.currentLocation != mine)
                    continue;

                mine.SpawnMonstersWithin(
                    monstersToSpawn, farmer.Tile, NearestSpawn, FurthestSpawn, chasersOnly: true);
            }
        }

        public override void Update(MineShaft mine, GameTime time)
        {
            if (!Context.IsMainPlayer || IsOver || !Game1.shouldTimePass())
                return;

            if (!spawnedFirstWave)
            {
                SpawnWave(mine);
                spawnedFirstWave = true;
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
}

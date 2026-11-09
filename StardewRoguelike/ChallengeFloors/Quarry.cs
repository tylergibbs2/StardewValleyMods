using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewRoguelike.Extensions;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;
using System.Collections.Generic;

namespace StardewRoguelike.ChallengeFloors
{
    /// <summary>
    /// A floor of rock, on a clock, that sends something after the player for every
    /// few rocks they break.
    ///
    /// Prospector had nowhere to be. It pays out on breaking stone, and the only
    /// floors with stone on them are the quiet ones - a mineshaft is not a fight, so
    /// the keystone that wants rock was the one keystone a combat floor could never
    /// notice. A player who took it was choosing to be weaker on every floor the run
    /// actually tested them on.
    ///
    /// So this floor is rock and a fight at once. Breaking stone is the score and
    /// breaking stone is what brings the monsters, which means Prospector's burst
    /// goes off exactly where the thing it is supposed to kill is standing. The
    /// pickaxe every player starts with is already fully upgraded, so no rock takes
    /// more than one swing: what the floor measures is how well the player can route
    /// a map while something is chasing them, not how long they can hold a button.
    /// </summary>
    internal class Quarry : ChallengeBase
    {
        /// <summary>The chance a clear tile gets a rock.</summary>
        private const double StoneChance = 0.25;

        /// <summary>At most this many rocks, however big the map is.</summary>
        /// <remarks>
        /// A cap rather than a density alone. The map list runs from small arenas to
        /// the wide custom ones, and a density that filled the small ones sensibly
        /// put over four hundred objects on the large ones - which is a floor nobody
        /// can clear inside the time and a frame cost for everyone standing on it.
        ///
        /// Forty, down from a hundred and thirty. The first number made clearing the
        /// floor a job rather than a run through it: every rock breaks in one swing,
        /// so a hundred and thirty of them is a hundred and thirty swings and no
        /// decision anywhere in it. Forty is a route the player can see the end of
        /// when they arrive, which is what makes hurrying through it worth anything.
        /// </remarks>
        private const int MaxStones = 40;

        /// <summary>One rock in this many is an ore node rather than stone.</summary>
        private const int OreInterval = 6;

        /// <summary>One rock in this many is worth a gem.</summary>
        private const int GemInterval = 9;

        /// <summary>Seconds on the clock.</summary>
        private const int FloorSeconds = 75;

        /// <summary>How many rocks have to break to bring another monster.</summary>
        /// <remarks>
        /// Five rather than seven, now that there are forty rocks instead of a
        /// hundred and thirty. Kept at seven, the smaller floor sent five monsters
        /// where the larger one sent eighteen, and a quarry nothing comes to is a
        /// mining floor with a clock on it.
        /// </remarks>
        private const int StonesPerMonster = 5;

        private readonly NetInt floorSecondsLeft = new(FloorSeconds);

        private readonly NetInt broken = new(0);

        private readonly NetInt stonesPlaced = new(0);

        private bool placedStone = false;

        /// <summary>The tiles this floor put rock on, so what is left can be counted.</summary>
        private readonly List<Vector2> stoneTiles = new();

        private int monstersSent = 0;

        private int tickCounter = 0;

        public Quarry() : base() { }

        protected override void InitNetFields()
        {
            base.InitNetFields();

            NetFields.AddField(floorSecondsLeft).AddField(broken).AddField(stonesPlaced);
        }

        public override List<string> MapPaths => new() {
            "3", "21", "23", "custom-1",
            "custom-2", "custom-3", "custom-4",
            "custom-5", "custom-6", "custom-7"
        };

        public override bool ShouldSpawnLadder(MineShaft mine) => IsOver;

        /// <summary>Lays the rock out over the whole map.</summary>
        /// <remarks>
        /// populateLevel leaves challenge floors bare - it returns early for anything
        /// that is not a normal floor - so the rock here is this floor's own, and
        /// nothing else on the map will be competing for the tiles.
        ///
        /// Candidates are collected and shuffled before any are used, so that capping
        /// the count thins the rock out across the map rather than filling the top of
        /// it and leaving the bottom bare.
        /// </remarks>
        private void PlaceStone(MineShaft mine)
        {
            placedStone = true;

            List<Vector2> candidates = new();
            for (int x = 0; x < mine.map.GetLayer("Back").LayerWidth; x++)
            {
                for (int y = 0; y < mine.map.GetLayer("Back").LayerHeight; y++)
                {
                    if (!mine.isTileClearForMineObjects(x, y))
                        continue;

                    // Clear of the ladder, so the player is not walled in where they
                    // land and so the reward has somewhere to go at the end.
                    if (mine.getDistanceFromStart(x, y) <= 2f)
                        continue;

                    if (Game1.random.NextDouble() <= StoneChance)
                        candidates.Add(new(x, y));
                }
            }

            candidates.Shuffle(Game1.random);

            int placed = 0;
            foreach (Vector2 tile in candidates)
            {
                if (placed == MaxStones)
                    break;

                if (mine.Objects.ContainsKey(tile))
                    continue;

                if (placed % OreInterval == OreInterval - 1)
                {
                    // Does its own placement check, and quietly does nothing if the
                    // tile will not take an object.
                    mine.placeAppropriateOreAt(tile);

                    if (!mine.Objects.ContainsKey(tile))
                        continue;
                }
                else
                {
                    string stoneId = placed % GemInterval == GemInterval - 1
                        ? mine.getRandomGemRichStoneForThisLevel(mine.mineLevel)
                        : Constants.QuarryStoneIds[Game1.random.Next(Constants.QuarryStoneIds.Count)];

                    mine.Objects.Add(tile, new SObject(stoneId, 1)
                    {
                        TileLocation = tile,
                        MinutesUntilReady = 1
                    });
                }

                stoneTiles.Add(tile);
                placed++;
            }

            stonesPlaced.Value = placed;

            // Vanilla reads this when a rock breaks to decide whether to drop a
            // ladder. The mod's own gate already answers that for challenge floors,
            // but leaving it at zero makes every rock the last one, which changes
            // what the base game does on the way through.
            mine.stonesLeftOnThisLevel = placed;
        }

        /// <summary>How much of this floor's rock is still standing.</summary>
        /// <remarks>
        /// Counted off the tiles the floor itself filled, rather than off a break
        /// event or off everything on the map. breakStone runs on whichever player
        /// swung and a farmhand cannot write the host's net fields, so a count kept
        /// there would have missed everything the other players did; counting every
        /// object instead would have counted the reward chest and anything a player
        /// put down.
        /// </remarks>
        private int StoneRemaining(MineShaft mine)
        {
            int standing = 0;

            foreach (Vector2 tile in stoneTiles)
            {
                if (mine.Objects.ContainsKey(tile))
                    standing++;
            }

            return standing;
        }

        private void Settle(MineShaft mine)
        {
            Finish();

            mine.characters.RemoveWhere(c => c is Monster);
            mine.createLadderAt(ChallengeFloor.GetSpawnLocation(mine));

            float share = stonesPlaced.Value == 0 ? 0f : broken.Value / (float)stonesPlaced.Value;

            if (share < 0.2f)
            {
                mine.playSound("cowboy_dead");
                return;
            }

            mine.playSound(share >= 1f ? "Cowboy_Secret" : "discoverMineral");

            // Bars rather than gems: everything the floor already handed over was ore,
            // and a bar is the one thing a run cannot get out of a rock by itself.
            int bar = share switch
            {
                >= 1f => 337,    // iridium
                >= 0.75f => 336, // gold
                >= 0.45f => 335, // iron
                _ => 334         // copper
            };

            List<Item> reward = new()
            {
                new SObject(bar.ToString(), share >= 0.75f ? 2 : 1),
                new SObject("384", share >= 1f ? 50 : 20)
            };

            Vector2 spawn = ChallengeFloor.GetSpawnLocation(mine);
            mine.SpawnLocalChest(RewardSpot(mine, spawn), reward);
        }

        private void RenderHud(object? sender, RenderedHudEventArgs e)
        {
            DrawHudReading(e.SpriteBatch, 0, I18n.ChallengeFloor_Shared_TimeLeft(seconds: floorSecondsLeft.Value));
            DrawHudReading(e.SpriteBatch, 1, I18n.ChallengeFloor_Quarry_Broken(
                amount: broken.Value, total: stonesPlaced.Value));
        }

        public override void PlayerEntered(MineShaft mine)
        {
            base.PlayerEntered(mine);
            Game1.chatBox.addMessage(I18n.ChallengeFloor_Quarry_WelcomeMessage(), Color.Gold);

            ModEntry.Events.Display.RenderedHud += RenderHud;
        }

        public override void PlayerLeft(MineShaft mine)
        {
            base.PlayerLeft(mine);
            Dispose();
        }

        public override void Update(MineShaft mine, GameTime time)
        {
            if (!Context.IsMainPlayer || IsOver || !Game1.shouldTimePass())
                return;

            if (!placedStone)
            {
                PlaceStone(mine);
                return;
            }

            broken.Value = stonesPlaced.Value - StoneRemaining(mine);

            // One monster per so many rocks, which puts the floor's difficulty in the
            // player's own hands: digging faster is worth more and costs more.
            while (monstersSent < broken.Value / StonesPerMonster)
            {
                // The one thing a woken monster has to do is come and interrupt the
                // mining; a rock crab sitting across the map does not.
                mine.SpawnMonsters(1, chasersOnly: true);
                monstersSent++;
            }

            tickCounter++;
            if (tickCounter < 60)
                return;

            tickCounter = 0;

            if (broken.Value >= stonesPlaced.Value || floorSecondsLeft.Value == 0)
                Settle(mine);
            else
                floorSecondsLeft.Value--;
        }

        public override void Dispose()
        {
            ModEntry.Events.Display.RenderedHud -= RenderHud;
        }
    }
}

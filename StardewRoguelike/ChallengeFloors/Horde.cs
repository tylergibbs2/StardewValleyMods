using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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
    /// A floor packed with slimes that die to anything.
    ///
    /// Every other combat floor asks the same question - can the player put enough
    /// damage into one monster at a time - so a run answered it once at the merchant
    /// and never again. The keystones made how a build spends its damage worth
    /// asking about: Volatile wants a crowd to chain through, Flurry wants a streak
    /// it never has to break, Momentum wants kills coming in faster than its stacks
    /// fall off. None of them had a floor that noticed.
    ///
    /// So the monsters here have one hit point each and there are sixty of them.
    /// Nothing survives being hit, which takes single-target damage out of the
    /// question entirely and leaves only how much of the room a swing covers. A
    /// dagger build clears it and a club build does not, and the reward says which.
    /// </summary>
    internal class Horde : ChallengeBase
    {
        /// <summary>How many slimes, before the player count and the curse.</summary>
        private const int SwarmSize = 60;

        /// <summary>Seconds on the clock.</summary>
        private const int FloorSeconds = 45;

        /// <summary>
        /// What one slime does on contact, before the floor's difficulty.
        /// </summary>
        /// <remarks>
        /// They have to hurt or the floor is a field of wheat. It is deliberately
        /// less than a normal monster of the depth: being hit sixty times by
        /// something that cannot be killed fast enough is the failure state, and at a
        /// normal monster's damage that failure arrives before the player has read
        /// the room.
        /// </remarks>
        private const int ContactDamage = 4;

        private readonly NetInt floorSecondsLeft = new(FloorSeconds);

        private readonly NetInt killed = new(0);

        private readonly NetInt swarmSize = new(0);

        private bool spawnedSwarm = false;

        private int tickCounter = 0;

        public Horde() : base() { }

        protected override void InitNetFields()
        {
            base.InitNetFields();

            NetFields.AddField(floorSecondsLeft).AddField(killed).AddField(swarmSize);
        }

        public override List<string> MapPaths => new() {
            "3", "21", "23", "custom-1",
            "custom-2", "custom-3", "custom-4",
            "custom-5", "custom-6", "custom-7"
        };

        public override bool ShouldSpawnLadder(MineShaft mine) => IsOver;

        /// <summary>Fills the map with slimes, as far from the ladder as it can.</summary>
        /// <remarks>
        /// Candidates are collected and then shuffled rather than placed as the sweep
        /// finds them. SpawnMonsters places on a per-tile chance, which on a map this
        /// crowded piles the swarm into whichever corner the sweep reaches first; the
        /// floor only reads as a swarm if it is one everywhere.
        /// </remarks>
        private void SpawnSwarm(MineShaft mine)
        {
            spawnedSwarm = true;

            int target = SwarmSize * Game1.getOnlineFarmers().Count;
            if (Curse.AnyFarmerHasCurse(CurseType.MoreEnemiesLessHealth))
                target += target / 4;

            List<Vector2> candidates = new();
            for (int x = 0; x < mine.map.GetLayer("Back").LayerWidth; x++)
            {
                for (int y = 0; y < mine.map.GetLayer("Back").LayerHeight; y++)
                {
                    if (mine.isTileClearForMineObjects(x, y) && mine.getDistanceFromStart(x, y) > 5f)
                        candidates.Add(new(x, y));
                }
            }

            candidates.Shuffle(Game1.random);

            float difficulty = BossFloor.GetLevelDifficulty(Roguelike.GetLevelFromMineshaft(mine));
            int placed = 0;

            foreach (Vector2 tile in candidates)
            {
                if (placed == target)
                    break;

                GreenSlime slime = new(tile * 64f, 0)
                {
                    // One hit point, and nothing that would give it a second chance.
                    // A slime that survived a swing would make this a damage check
                    // again, which is the floor it was built to not be.
                    MaxHealth = 1,
                    Health = 1,
                    DamageToFarmer = (int)System.Math.Round(ContactDamage * difficulty)
                };
                slime.resilience.Value = 0;

                if (mine.TryToAddMonster(slime, (int)tile.X, (int)tile.Y))
                    placed++;
            }

            swarmSize.Value = placed;
        }

        /// <summary>Settles the floor and pays for what the player got through.</summary>
        /// <remarks>
        /// Paid on a share of the swarm rather than a count, because the swarm is
        /// scaled by how many players are on the floor and a count would make a
        /// second player a pay cut. Clearing it is a tier of its own: the gap between
        /// killing almost all of them and killing all of them is where a build that
        /// covers the room stops being merely better and starts being the point.
        /// </remarks>
        private void Settle(MineShaft mine)
        {
            Finish();

            mine.characters.RemoveWhere(c => c is Monster);
            mine.createLadderAt(ChallengeFloor.GetSpawnLocation(mine));

            float share = swarmSize.Value == 0 ? 0f : killed.Value / (float)swarmSize.Value;

            if (share < 0.25f)
            {
                mine.playSound("cowboy_dead");
                return;
            }

            mine.playSound(share >= 1f ? "Cowboy_Secret" : "discoverMineral");

            // Gems rather than ore: the floor has no stone on it, so ore here would
            // be the only place in a run that hands over a thing to be refined with
            // nothing to refine it for.
            int gem = share switch
            {
                >= 1f => 72,     // diamond
                >= 0.8f => 64,   // ruby
                >= 0.5f => 60,   // emerald
                _ => 66          // amethyst
            };

            int gold = share switch
            {
                >= 1f => 60,
                >= 0.8f => 40,
                >= 0.5f => 25,
                _ => 15
            };

            List<Item> reward = new()
            {
                new SObject(gem.ToString(), 1),
                new SObject("384", gold)
            };

            Vector2 spawn = ChallengeFloor.GetSpawnLocation(mine);
            mine.SpawnLocalChest(RewardSpot(mine, spawn), reward);
        }

        private void RenderHud(object? sender, RenderedHudEventArgs e)
        {
            DrawHudReading(e.SpriteBatch, 0, I18n.ChallengeFloor_Shared_TimeLeft(seconds: floorSecondsLeft.Value));
            DrawHudReading(e.SpriteBatch, 1, I18n.ChallengeFloor_Horde_Remaining(
                amount: System.Math.Max(0, swarmSize.Value - killed.Value)));
        }

        public override void PlayerEntered(MineShaft mine)
        {
            base.PlayerEntered(mine);
            Game1.chatBox.addMessage(I18n.ChallengeFloor_Horde_WelcomeMessage(), Color.Gold);

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

            if (!spawnedSwarm)
            {
                SpawnSwarm(mine);
                return;
            }

            // Counted off what is left alive rather than off a kill event, so a slime
            // that an explosion took, or that a farmhand killed, counts the same as
            // one the host hit.
            killed.Value = swarmSize.Value - mine.EnemyCount;

            tickCounter++;
            if (tickCounter < 60)
                return;

            tickCounter = 0;

            if (mine.EnemyCount == 0 || floorSecondsLeft.Value == 0)
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

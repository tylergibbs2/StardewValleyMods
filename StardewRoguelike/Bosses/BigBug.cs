using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewRoguelike.Extensions;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;
using System;
using System.Collections.Generic;

namespace StardewRoguelike.Bosses
{
    public class BigBug : Bat, IBossMonster
    {
        public string DisplayName => "Odys the Hivemother";

        public string MapPath => "boss-bug";

        public string TextureName => "Characters\\Monsters\\Bug_dangerous";

        public Vector2 SpawnLocation => new(17, 23);

        public List<string> MusicTracks
        => new() { "ceaseless_and_incessant" };

        public bool InitializeWithHealthbar
        => true;

        public float Difficulty { get; set; }

        private enum State
        {
            Normal,
            MovingToEgg,
            LayingEgg
        }

        public enum MinionState
        {
            Normal,
            Fast,
            Debuffing,
            Defensive,
            Aggressive,
            Suicidal
        }

        private readonly NetEnum<State> CurrentState = new(State.Normal);

        private MinionState PreviousMinionState;

        private readonly NetEnum<MinionState> CurrentMinionState = new(MinionState.Normal);

        private readonly Dictionary<Vector2, int> Eggs = new();

        private readonly NetEvent1Field<Vector2, NetVector2> SpawnEggEvent = new();

        private readonly NetEvent1Field<Vector2, NetVector2> despawnEggEvent = new();

        private readonly NetEvent1Field<Vector2, NetVector2> squashEggEvent = new();

        /// <summary>
        /// Where the eggs are, as every machine sees it.
        ///
        /// <see cref="Eggs"/> is the host's clock and is never synced, so a farmhand
        /// has no idea where the eggs are and could not swing at one. The tiles are
        /// laid down on every machine by <see cref="SpawnEgg"/>, so this is kept
        /// beside them: the positions everybody knows, without the fuses only the
        /// host needs.
        /// </summary>
        private readonly HashSet<Vector2> eggTiles = new();

        private readonly NetEvent0 OnAttackedEvent = new();

        private Vector2 TargetEggSpot;

        private int TicksToLayEgg = 300;

        private int StateTickCount = 0;

        private int TicksToChangeMinions = 800;

        private int TicksToOffscreenBugs = 180;

        private readonly List<Vector2> EggSpawnLocations = new()
        {
            new(14, 10),
            new(25, 11),
            new(33, 14),
            new(10, 16),
            new(16, 21),
            new(30, 27),
            new(10, 26),
            new(13, 34),
            new(23, 25)
        };

        public BigBug() { }

        public BigBug(float difficulty) : base(Vector2.Zero)
        {
            setTileLocation(SpawnLocation);
            Difficulty = difficulty;

            Sprite.LoadTexture(TextureName);
            Sprite.SpriteHeight = 16;
            Sprite.UpdateSourceRect();
            Scale = 3f;

            moveTowardPlayerThreshold.Value = 20;
        }

        protected override void initNetFields()
        {
            base.initNetFields();

            // CurrentState and CurrentMinionState were declared as NetEnums but never
            // registered, so they never left the host. Farmhands read Normal all
            // fight: takeDamage skipped the halving that makes the boss tough while
            // it is laying, and updateAnimation - which runs on every machine - never
            // showed the laying pose.
            NetFields.AddField(SpawnEggEvent).AddField(despawnEggEvent).AddField(OnAttackedEvent)
                .AddField(CurrentState).AddField(CurrentMinionState).AddField(squashEggEvent);
            SpawnEggEvent.onEvent += SpawnEgg;
            despawnEggEvent.onEvent += DespawnEgg;
            squashEggEvent.onEvent += SquashEgg;
            OnAttackedEvent.onEvent += OnAttacked;
        }

        public override void reloadSprite(bool onlyAppearance = false)
        {
            Sprite = new(TextureName);
            Sprite.LoadTexture(TextureName);
            Sprite.SpriteHeight = 16;
            Sprite.UpdateSourceRect();
            HideShadow = true;
        }

        public void OnAttacked()
        {
            if (Context.IsMainPlayer && CurrentState.Value == State.LayingEgg)
            {
                CurrentState.Value = State.Normal;
                TicksToLayEgg = 300;
                StateTickCount = 0;
            }
        }

        public override void MovePosition(GameTime time, xTile.Dimensions.Rectangle viewport, GameLocation currentLocation)
        {
            if (CurrentState.Value == State.Normal)
                base.MovePosition(time, viewport, currentLocation);
        }

        public override void behaviorAtGameTick(GameTime time)
        {
            CheckAllEggs();

            if (TicksToOffscreenBugs > 0)
            {
                TicksToOffscreenBugs--;

                if (TicksToOffscreenBugs == 0)
                {
                    int toSpawn = Game1.random.Next(1, 3);
                    SpawnOffScreenBugs(toSpawn);

                    TicksToOffscreenBugs = Game1.random.Next(6 * 60, 10 * 60);
                    if (Roguelike.HardMode)
                        TicksToOffscreenBugs -= Game1.random.Next(60, 3 * 60);
                }
            }

            if (TicksToChangeMinions > 0)
            {
                TicksToChangeMinions--;

                if (TicksToChangeMinions == 0)
                {
                    if (CurrentMinionState.Value != MinionState.Normal)
                    {
                        PreviousMinionState = CurrentMinionState.Value;
                        CurrentMinionState.Value = MinionState.Normal;
                        TicksToChangeMinions = 300;
                    }
                    else
                    {
                        var validStates = new List<MinionState>((IEnumerable<MinionState>)Enum.GetValues(typeof(MinionState)));
                        validStates.Remove(PreviousMinionState);

                        CurrentMinionState.Value = validStates[Game1.random.Next(validStates.Count)];

                        TicksToChangeMinions = Roguelike.HardMode ? 480 : 600;
                    }

                    SetAllMinionStates(CurrentMinionState.Value);
                }
            }

            if (CurrentState.Value == State.Normal)
            {
                base.behaviorAtGameTick(time);

                if (TicksToLayEgg > 0)
                {
                    TicksToLayEgg--;

                    if (TicksToLayEgg == 0)
                    {
                        // There are ten spots and an egg is laid every two seconds,
                        // while the only thing that frees one up is the player
                        // breaking an egg. Once they are all taken the loop below has
                        // no exit and the game hangs, so wait instead of searching for
                        // a spot that does not exist.
                        if (Eggs.Count >= EggSpawnLocations.Count)
                        {
                            TicksToLayEgg = 60;
                        }
                        else
                        {
                            do
                            {
                                TargetEggSpot = EggSpawnLocations[Game1.random.Next(EggSpawnLocations.Count)];
                            } while (Eggs.ContainsKey(TargetEggSpot));

                            TicksToLayEgg = 120;
                            CurrentState.Value = State.MovingToEgg;
                            StateTickCount = 0;
                        }
                    }
                }
            }
            else if (CurrentState.Value == State.MovingToEgg)
            {
                Vector2 moveVector = (new Vector2(TargetEggSpot.X + 1, TargetEggSpot.Y + 1) * 64f) - GetBoundingBox().Center.ToVector2();

                if (moveVector.LengthSquared() <= 90)
                {
                    CurrentState.Value = State.LayingEgg;
                    StateTickCount = 0;
                    return;
                }

                moveVector.Normalize();
                moveVector *= Roguelike.HardMode ? 11f : 8f;

                Position += moveVector;
                rotation = RoguelikeUtility.VectorToRadians(moveVector) + RoguelikeUtility.DegreesToRadians(90);
            }
            else if (CurrentState.Value == State.LayingEgg)
            {
                if (StateTickCount > 120)
                {
                    Eggs[TargetEggSpot] = 500;
                    SpawnEggEvent.Fire(TargetEggSpot);

                    CurrentState.Value = State.Normal;
                    StateTickCount = 0;
                }
            }

            StateTickCount++;
        }

        public void CheckAllEggs()
        {
            // Over a copy of the keys. Firing the despawn takes the egg straight back
            // out of this dictionary - NetEvent.Fire polls itself, so the handler runs
            // before Fire returns - and relying on Dictionary.Remove being allowed
            // mid-enumeration is a .NET implementation detail to be leaning on in a
            // method that hatches things.
            foreach (Vector2 tileLocation in new List<Vector2>(Eggs.Keys))
            {
                if (Eggs[tileLocation] == 0)
                    continue;

                Eggs[tileLocation]--;
                if (Eggs[tileLocation] == 0)
                {
                    despawnEggEvent.Fire(tileLocation);
                    SpawnBugs(tileLocation);
                }
            }
        }

        public void SetAllMinionStates(MinionState newState)
        {
            if (newState != MinionState.Normal)
                currentLocation.playSound("shadowpeep");

            foreach (Character character in currentLocation.characters)
            {
                if (character is BigBugMinion minion)
                    minion.ChangeState(newState);
            }
        }

        /// <summary>How many of its bugs one player may be facing at once.</summary>
        /// <remarks>
        /// Nothing bounded the swarm. Nine egg sites, each hatching two or three, with
        /// more drifting in off the edges every few seconds and the boss retrying the
        /// moment the board was full - so a player who fell behind did not fall behind
        /// by a fixed amount, they fell behind faster the further behind they got.
        ///
        /// Per player, because the spawn counts are per player too: the swarm is the
        /// fight, and a party that outnumbers it is not having one.
        /// </remarks>
        private const int MinionsPerPlayer = 12;

        private static int PlayerCount => Math.Max(1, Game1.getOnlineFarmers().Count);

        /// <summary>How much room is left under the cap.</summary>
        private int MinionRoom()
        {
            int alive = 0;
            foreach (NPC character in currentLocation.characters)
            {
                if (character is BigBugMinion minion && minion.Health > 0)
                    alive++;
            }

            return MinionsPerPlayer * PlayerCount - alive;
        }

        public void SpawnOffScreenBugs(int amount)
        {
            amount = Math.Min(amount * PlayerCount, MinionRoom());

            while (amount > 0)
            {
                Vector2 spawnLocation = Vector2.Zero;
                switch (Game1.random.Next(4))
                {
                    case 0:
                        spawnLocation.X = Game1.random.Next(currentLocation.map.Layers[0].LayerWidth);
                        break;
                    case 3:
                        spawnLocation.Y = Game1.random.Next(currentLocation.map.Layers[0].LayerHeight);
                        break;
                    case 1:
                        spawnLocation.X = currentLocation.map.Layers[0].LayerWidth - 1;
                        spawnLocation.Y = Game1.random.Next(currentLocation.map.Layers[0].LayerHeight);
                        break;
                    case 2:
                        spawnLocation.Y = currentLocation.map.Layers[0].LayerHeight - 1;
                        spawnLocation.X = Game1.random.Next(currentLocation.map.Layers[0].LayerWidth);
                        break;
                }
                if (Utility.isOnScreen(spawnLocation * 64f, 64))
                    spawnLocation.X -= Game1.viewport.Width / 64;

                BigBugMinion minion = new(spawnLocation, Difficulty, CurrentMinionState.Value);
                minion.moveTowardPlayerThreshold.Value = 100;
                minion.focusedOnFarmers = true;
                currentLocation.characters.Add(minion);

                amount--;
            }
        }

        public void SpawnBugs(Vector2 tileLocation)
        {
            int bugsToSpawn = Game1.random.Next(2, 4);
            if (Curse.AnyFarmerHasCurse(CurseType.MoreEnemiesLessHealth))
                bugsToSpawn++;

            // Per player, and never past the cap. The counts were written for one
            // player and stayed that way however many turned up, so the swarm - the
            // only thing in this fight that is not a health bar - was the first thing
            // co-op turned off.
            bugsToSpawn = Math.Min(bugsToSpawn * PlayerCount, MinionRoom());
            if (bugsToSpawn <= 0)
                return;

            tileLocation *= 64;

            while (bugsToSpawn > 0)
            {
                tileLocation = new(tileLocation.X + 64 + Game1.random.Next(-32, 33), tileLocation.Y + 64 + Game1.random.Next(-32, 33));
                BigBugMinion minion = new(tileLocation, Difficulty, CurrentMinionState.Value);
                currentLocation.characters.Add(minion);

                bugsToSpawn--;
            }
        }

        public void SpawnEgg(Vector2 tileLocation)
        {
            eggTiles.Add(tileLocation);

            MineShaft mine = (MineShaft)currentLocation;
            mine.SetTile((int)tileLocation.X, (int)tileLocation.Y, "Front", "bugLandTiles", 92, 1000, new int[] { 92, 86, 92, 102 });
            mine.SetTile((int)tileLocation.X + 1, (int)tileLocation.Y, "Front", "bugLandTiles", 93, 1000, new int[] { 93, 87, 93, 103 });
            mine.SetTile((int)tileLocation.X, (int)tileLocation.Y + 1, "Front", "bugLandTiles", 100, 1000, new int[] { 100, 94, 100, 110 });
            mine.SetTile((int)tileLocation.X + 1, (int)tileLocation.Y + 1, "Front", "bugLandTiles", 101, 1000, new int[] { 101, 95, 101, 111 });

            Game1.playSound("fishSlap");
        }

        /// <summary>Takes the egg off the map, however it came to an end.</summary>
        /// <remarks>
        /// Shared by hatching and squashing so the two cannot fall out of step over
        /// which tiles an egg occupies, and so only the part that differs - the sound,
        /// and whether anything is left behind - lives in the two callers.
        /// </remarks>
        private void RemoveEggTiles(Vector2 tileLocation)
        {
            eggTiles.Remove(tileLocation);

            MineShaft mine = (MineShaft)currentLocation;
            xTile.Dimensions.Location tile = new((int)tileLocation.X, (int)tileLocation.Y);
            mine.removeTile(tile, "Front");
            mine.removeTile(new(tile.X + 1, tile.Y), "Front");
            mine.removeTile(new(tile.X, tile.Y + 1), "Front");
            mine.removeTile(new(tile.X + 1, tile.Y + 1), "Front");

            if (Context.IsMainPlayer)
                Eggs.Remove(tileLocation);
        }

        public void DespawnEgg(Vector2 tileLocation)
        {
            RemoveEggTiles(tileLocation);
            Game1.playSound("slimeHit");
        }

        /// <summary>
        /// Whether a swing reached an egg, and squashes the first one it did.
        /// </summary>
        /// <remarks>
        /// The egg was the one thing in this fight with no answer to it: it was laid
        /// with a fuse and hatched whatever the player did, so the only counterplay
        /// was catching the boss mid-lay at a spot it had flown off to, and a player
        /// who missed that window lost to the arena filling up rather than to
        /// anything they could see going wrong.
        ///
        /// Fired as an event rather than resolved here, because the fuse lives on the
        /// host and the tiles live on every machine - and the player who swung may be
        /// neither.
        /// </remarks>
        public bool TrySquashEgg(Rectangle area)
        {
            Vector2? hit = null;

            foreach (Vector2 tileLocation in eggTiles)
            {
                // Two tiles square, laid from its top-left corner.
                Rectangle egg = new((int)tileLocation.X * 64, (int)tileLocation.Y * 64, 128, 128);

                if (area.Intersects(egg))
                {
                    hit = tileLocation;
                    break;
                }
            }

            if (hit is null)
                return false;

            // Fired after the search rather than inside it. NetEvent.Fire polls
            // itself, so the handler - which takes the egg out of this very set -
            // runs before Fire returns, and firing mid-loop threw on the collection
            // having been modified.
            squashEggEvent.Fire(hit.Value);
            return true;
        }

        /// <summary>An egg that was stood on rather than left to hatch.</summary>
        public void SquashEgg(Vector2 tileLocation)
        {
            RemoveEggTiles(tileLocation);

            Vector2 center = new((tileLocation.X + 1) * 64f, (tileLocation.Y + 1) * 64f);

            Game1.playSound("slimedead");

            // Chunks off the boss's own sheet, so what bursts out of an egg looks
            // like what would have crawled out of it.
            Game1.createRadialDebris(
                currentLocation, TextureName, new Rectangle(0, 0, 16, 16), 8,
                (int)center.X, (int)center.Y, 10, (int)tileLocation.Y + 1,
                Color.White, 3f);

            Utility.makeTemporarySpriteJuicier(
                new TemporaryAnimatedSprite(44, center - new Vector2(32f, 32f), Color.YellowGreen, 10)
                {
                    interval = 55f,
                    scale = 0.8f
                },
                currentLocation);
        }

        protected override void updateAnimation(GameTime time)
        {
            if (CurrentState.Value == State.LayingEgg)
            {
                Sprite.currentFrame = 4;
                Halt();
                return;
            }

            base.updateAnimation(time);
        }

        public override void update(GameTime time, GameLocation location)
        {
            this.KeepInMap();
            base.update(time, location);
            SpawnEggEvent.Poll();
            despawnEggEvent.Poll();
            squashEggEvent.Poll();
            OnAttackedEvent.Poll();
        }

        public override Rectangle GetBoundingBox()
        {
            int boxWidth = (int)(Sprite.SpriteWidth * 4 * 3 / 4 * Scale);
            int boxHeight = (int)(26 * Scale);
            return new((int)Position.X - (boxWidth / 4), (int)Position.Y + boxHeight / 3, boxWidth, boxHeight);
        }

        public override void shedChunks(int number, float scale)
        {
            Game1.createRadialDebris(currentLocation, Sprite.textureName.Value, new Rectangle(0, Sprite.SpriteHeight * 4, Sprite.SpriteWidth, Sprite.SpriteHeight), Sprite.SpriteWidth / 2, GetBoundingBox().Center.X, GetBoundingBox().Center.Y, number, (int)Tile.Y, Color.White, 4f);
        }

        public override int takeDamage(int damage, int xTrajectory, int yTrajectory, bool isBomb, double addedPrecision, Farmer who)
        {
            OnAttackedEvent.Fire();

            if (CurrentState.Value != State.Normal)
                damage /= 2;

            int result = base.takeDamage(damage, xTrajectory, yTrajectory, isBomb, addedPrecision, who);
            if (Health <= 0)
                BossManager.Death(currentLocation, who, DisplayName, SpawnLocation);

            return result;
        }
    }
}

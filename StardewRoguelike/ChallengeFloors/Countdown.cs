using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewRoguelike.Extensions;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;
using System.Collections.Generic;
using System.Linq;

namespace StardewRoguelike.ChallengeFloors
{
    /// <summary>
    /// A floor with the ladder open from the first second and waves that never stop
    /// coming.
    ///
    /// Every other challenge floor is a threshold: clear the room, beat the clock,
    /// hold the box. The player either meets it or does not, and in both cases the
    /// floor decides when they are finished with it. That makes eight floors' worth
    /// of tests of execution and not one test of judgement.
    ///
    /// Here the exit is available immediately and the waves get bigger and harder
    /// without end, paying out a chest at the ladder for each one cleared. There is
    /// no winning it. The only question the floor asks is when to stop, and it asks
    /// it again every thirty seconds, with a better answer each time and a worse
    /// consequence for being wrong - because dying here costs the run, not the floor.
    /// </summary>
    internal class Countdown : ChallengeBase
    {
        /// <summary>Monsters in the first wave; each wave after adds one.</summary>
        private const int BaseWaveSize = 3;

        /// <summary>How much tougher each wave is than the one before.</summary>
        /// <remarks>
        /// Steeply enough that nobody farms it. The floor has no clock and no clear
        /// condition, so the only thing stopping a strong build from standing here
        /// until it has every chest in the game is the monsters eventually being
        /// able to kill it. Fifteen per cent a wave compounds: wave ten hits around
        /// four times as hard as wave one.
        /// </remarks>
        private const float WaveScaling = 0.15f;

        /// <summary>Seconds of quiet between one wave dying and the next arriving.</summary>
        private const int WaveBreakSeconds = 3;

        /// <summary>
        /// The first wave worth a chest.
        /// </summary>
        /// <remarks>
        /// Not the first. A chest for clearing one small wave would make this the
        /// cheapest guaranteed reward in the run and the obvious pick every cycle,
        /// which is the trap the treasure floors were just dug out of. The player has
        /// to commit to a second wave before the floor pays anything.
        /// </remarks>
        private const int FirstPayingWave = 2;

        private readonly NetInt wave = new(0);

        private readonly NetInt chestsEarned = new(0);

        private readonly NetBool fighting = new(false);

        private bool openedExit = false;

        private int tickCounter = 0;

        private int breakSecondsLeft = 0;

        public Countdown() : base() { }

        protected override void InitNetFields()
        {
            base.InitNetFields();

            NetFields.AddField(wave).AddField(chestsEarned).AddField(fighting);
        }

        public override List<string> MapPaths => new() {
            "3", "21", "23", "custom-1",
            "custom-2", "custom-3", "custom-4",
            "custom-5", "custom-6", "custom-7"
        };

        /// <summary>
        /// The exit is open throughout, which is the whole floor.
        /// </summary>
        /// <remarks>
        /// Every other floor gates this on being finished. This one cannot be
        /// finished, and a greed check with the way out locked is not a greed check -
        /// it is a fight the player was not asked about.
        /// </remarks>
        public override bool ShouldSpawnLadder(MineShaft mine) => true;

        /// <summary>Sends the next wave, bigger and harder than the last.</summary>
        /// <remarks>
        /// The monsters standing before the spawn are noted so that only the new ones
        /// are scaled up. SpawnMonsters hands back nothing, and running the multiplier
        /// over everything on the floor would compound it onto whatever survived the
        /// last wave every time another arrived.
        /// </remarks>
        private void SpawnWave(MineShaft mine)
        {
            wave.Value++;
            fighting.Value = true;

            HashSet<Monster> before = new(mine.characters.OfType<Monster>());

            int count = BaseWaveSize + wave.Value;
            if (Curse.AnyFarmerHasCurse(CurseType.MoreEnemiesLessHealth))
                count++;

            // Chasers: a wave that will not come to the player is a wave that has
            // to be gone and found, on a floor whose whole shape is standing your
            // ground for one more wave.
            mine.SpawnMonsters(count * Game1.getOnlineFarmers().Count, chasersOnly: true);

            float scale = 1f + (wave.Value - 1) * WaveScaling;

            foreach (Monster monster in mine.characters.OfType<Monster>())
            {
                if (before.Contains(monster))
                    continue;

                monster.MaxHealth = (int)System.Math.Round(monster.MaxHealth * scale);
                monster.Health = monster.MaxHealth;
                monster.DamageToFarmer = (int)System.Math.Round(monster.DamageToFarmer * scale);
            }

            mine.playSound("cowboy_monsterhit");
        }

        /// <summary>Pays for the wave just cleared, at the ladder.</summary>
        /// <remarks>
        /// At the ladder rather than where the player is standing, so the winnings
        /// pile up in the one place they have to walk past to leave. What the floor
        /// is asking is whether to go, and the answer reads better next to the door.
        /// </remarks>
        private void PayForWave(MineShaft mine)
        {
            if (wave.Value < FirstPayingWave)
                return;

            chestsEarned.Value++;

            List<Item> reward = new();

            // Each band is what the wave before could not give, so staying is always
            // worth something the player could not have taken by leaving.
            if (wave.Value >= 8)
            {
                reward.Add(new SObject("337", 1));                 // iridium bar
                reward.Add(new SObject("384", 100));
            }
            else if (wave.Value >= 6)
            {
                reward.Add(new SObject("336", 1));                 // gold bar
                reward.Add(new SObject("72", 1));                  // diamond
            }
            else if (wave.Value >= 4)
            {
                reward.Add(new SObject("335", 1));                 // iron bar
                reward.Add(new SObject("64", 1));                  // ruby
            }
            else
            {
                reward.Add(new SObject("382", 5));                 // coal
                reward.Add(new SObject("384", 25));                // gold ore
            }

            Vector2 ladder = ChallengeFloor.GetSpawnLocation(mine);
            mine.SpawnLocalChest(RewardSpot(mine, ladder, maxRings: 6), reward);

            mine.playSound("discoverMineral");
        }

        private void RenderHud(object? sender, RenderedHudEventArgs e)
        {
            DrawHudReading(e.SpriteBatch, 0, I18n.ChallengeFloor_Countdown_Wave(amount: wave.Value));
            DrawHudReading(e.SpriteBatch, 1, I18n.ChallengeFloor_Countdown_Earned(amount: chestsEarned.Value));
        }

        public override void PlayerEntered(MineShaft mine)
        {
            base.PlayerEntered(mine);
            Game1.chatBox.addMessage(I18n.ChallengeFloor_Countdown_WelcomeMessage(), Color.Gold);

            ModEntry.Events.Display.RenderedHud += RenderHud;
        }

        public override void PlayerLeft(MineShaft mine)
        {
            base.PlayerLeft(mine);
            Dispose();
        }

        public override void Update(MineShaft mine, GameTime time)
        {
            if (!Context.IsMainPlayer || !Game1.shouldTimePass())
                return;

            if (!openedExit)
            {
                // Before the first wave, so the way out is never something the player
                // has to earn.
                mine.createLadderAt(ChallengeFloor.GetSpawnLocation(mine));
                openedExit = true;
            }

            // Nobody is on the floor any more. Without this the waves keep arriving
            // on an abandoned floor for as long as the mine stays loaded, and the
            // player walks back into whatever accumulated.
            if (mine.farmers.Count == 0)
            {
                mine.characters.RemoveWhere(c => c is Monster);
                fighting.Value = false;
                return;
            }

            tickCounter++;
            if (tickCounter < 60)
                return;

            tickCounter = 0;

            if (fighting.Value)
            {
                if (mine.EnemyCount > 0)
                    return;

                fighting.Value = false;
                PayForWave(mine);
                breakSecondsLeft = WaveBreakSeconds;
                return;
            }

            if (breakSecondsLeft > 0)
            {
                breakSecondsLeft--;
                return;
            }

            SpawnWave(mine);
        }

        public override void Dispose()
        {
            ModEntry.Events.Display.RenderedHud -= RenderHud;
        }
    }
}

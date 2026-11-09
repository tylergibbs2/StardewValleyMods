using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buffs;
using StardewValley.Monsters;
using StardewValley.Projectiles;
using StardewValley.Tools;
using System;
using System.Collections.Generic;

namespace StardewRoguelike
{
    /// <summary>
    /// What the keystone perks do when a monster dies.
    ///
    /// The other perks answer "how much": more damage, more health, more luck. These
    /// answer "what happens", which is the thing a run had none of - a player on the
    /// fortieth floor fought exactly as they had on the second, only harder. A
    /// keystone is taken at the first shrine of every run, so the floors after it are
    /// spent supporting a decision rather than collecting unrelated numbers.
    /// </summary>
    internal static class Keystones
    {
        /// <summary>What a keystone is part-way through for one player.</summary>
        /// <remarks>
        /// One object behind one <see cref="PerScreen{T}"/> rather than a dozen
        /// separate ones. These fields are all the same thing - what this player's
        /// keystones have accumulated - and a run begins by forgetting every one of
        /// them together, which is a single assignment here and was a list that had
        /// already fallen two fields behind.
        ///
        /// Per screen, not static, because all of it is one player's: a bank of
        /// damage, a run of untouched blows, a floor's second wind. Shared between
        /// two local players, one of them could spend the other's bank, and the
        /// re-entrancy guards were worse than merely wrong - the splash guard is read
        /// by the damage postfix to suppress leech and the crown, so one screen
        /// spreading a blow silenced the other screen's perks. The rest of the mod
        /// has kept run state per screen since perks did.
        /// </remarks>
        private sealed class State
        {
            /// <summary>Ticks since momentum last looked for something to hold it open.</summary>
            public int HoldTicks;

            /// <summary>Whether a burst is already breaking rock, so a chain stops at one.</summary>
            /// <remarks>
            /// Breaking a rock routes back through breakStone, which is where that is
            /// called from, so a burst that broke its neighbours would be called again
            /// for each of them. Left to run, one swing on a quarry floor takes the
            /// whole map and the game stops answering while it does. One ring deep: a
            /// swing clears a cluster, not a floor.
            /// </remarks>
            public bool Bursting;

            /// <summary>Damage taken since the last blow, waiting to be handed back.</summary>
            public int Banked;

            /// <summary>Blows landed since the player was last touched.</summary>
            public int Streak;

            /// <summary>Momentum's stack count, which decays with the buff.</summary>
            public int MomentumStacks;

            /// <summary>When that stack was last added, so it can lapse.</summary>
            public double MomentumRenewed;

            /// <summary>How many blasts deep the current chain already is.</summary>
            public int BlastDepth;

            /// <summary>How many blasts the kill being resolved has already set off.</summary>
            public int BlastsThisCascade;

            /// <summary>Whether the run's one forgiven hit has been used.</summary>
            public bool GraceSpent;

            /// <summary>What spite handed back on the last blow, for the splash to follow.</summary>
            public int SpitePaid;

            /// <summary>Whether a splash or a sweep is already running, so neither repeats.</summary>
            public bool Spreading;

            /// <summary>Whether this floor's second wind is still unspent.</summary>
            public bool SecondWindSpent;
        }

        private static readonly PerScreen<State> ScreenState = new(() => new());

        /// <summary>The keystone state of the player on this screen.</summary>
        private static State Current => ScreenState.Value;

        /// <summary>How far Volatile's blast reaches, in tiles.</summary>
        /// <remarks>
        /// Flat, and not per level. It used to be the level added to it, which read as
        /// a small number and was not one: the area a blast covers goes up with the
        /// square of this, so the fourth tier covered thirty-six times the ground the
        /// first did, and the chain doubled the reach again on top. Six tiles is
        /// between a bomb and a mega bomb - on every kill, twice over. A floor of
        /// sixty monsters went up from one swing.
        ///
        /// Depth scales the damage now and nothing else. Two tiles is a cherry bomb,
        /// three is the widened tier, and that is the whole range of it.
        /// </remarks>
        private const int BlastRadius = 2;

        /// <summary>What a chained blast keeps of the one that set it off.</summary>
        /// <remarks>
        /// A chain that was as wide and as hard as its parent made the cascade grow
        /// rather than die out, which is what turned a bounded depth into a cleared
        /// floor anyway. Halved, a chain reads as the blast carrying on and running
        /// out rather than as a second blast.
        /// </remarks>
        private const int BlastChainFalloff = 2;

        /// <summary>How many blasts one kill may set off in total.</summary>
        /// <remarks>
        /// The hard ceiling, and the one that does not care how the rest is tuned. The
        /// depth limit bounds how far a cascade reaches but not how much of it happens
        /// at each step, so on a crowded enough floor one kill still meant dozens of
        /// blasts. This is what keeps the worst case a function of the keystone rather
        /// than of how many monsters happened to be standing together.
        /// </remarks>
        private const int BlastBudget = 6;

        /// <summary>What Volatile's blast hits for, before the floor's difficulty.</summary>
        private const int BlastDamage = 25;

        /// <summary>How long whatever lived through a blast is left reeling, in ms.</summary>
        private const int BlastStunMs = 1500;

        /// <summary>How deep a chain of blasts may run.</summary>
        /// <remarks>
        /// One means the blast a kill causes, and nothing beyond it. Two is that blast
        /// plus the blast its own kills cause, which is what ChainKills buys and where
        /// it stops.
        ///
        /// It had no limit at all. A kill explodes, the explosion kills, and the kill
        /// hook this is called from fires again for each of those - so one blow on a
        /// crowded floor took the crowd and recursed a frame deep per monster while it
        /// did. Nobody noticed until a floor arrived with sixty monsters on it that die
        /// to anything, where it went from generous to the whole floor at once.
        /// </remarks>
        private const int BlastChainLimit = 1;

        private const int BlastChainLimitChained = 2;

        /// <summary>How long Momentum's stack lasts, and how high it stacks.</summary>
        private const int MomentumDurationMs = 4000;
        private const int MomentumMaxStacks = 5;

        /// <summary>How high it stacks once deepened.</summary>
        private const int MomentumDeepMaxStacks = 8;

        /// <summary>What a kill at full momentum gives back.</summary>
        private const int MomentumSustainHeal = 3;

        /// <summary>How near a monster has to be to hold momentum open, in tiles.</summary>
        private const int MomentumHoldRange = 10;

        /// <summary>The most momentum will ever give, however deep it is taken.</summary>
        /// <remarks>
        /// Stacks and level both used to multiply this, so the top tier handed out
        /// eight stacks times four levels - Speed 32 and Attack 32. Swift, this mod's
        /// own speed perk, gives one a level and stops at three; vanilla's strongest
        /// speed buff is one. Thirty-two was eleven times the whole of Swift, and fast
        /// enough to be unplayable rather than strong.
        ///
        /// Twice Swift for the speed, and an attack bonus in the range of the game's
        /// better rings. Depth still reaches the ceiling sooner and holds it more
        /// easily, which is what the stacking is for.
        /// </remarks>
        private const int MomentumSpeedCap = 6;

        private const int MomentumAttackCap = 12;

        /// <summary>How many ticks pass between one check for that and the next.</summary>
        private const int HoldCheckInterval = 30;

        /// <summary>The buff Momentum keeps on the player.</summary>
        private const string MomentumBuffId = "tylergibbs2.StardewRoguelike_Momentum";

        /// <summary>How likely Bombardier is to leave a bomb, per level.</summary>
        private const double BombChance = 0.15;

        /// <summary>How far Ricochet looks for something else to hit, in tiles.</summary>
        private const int RicochetRange = 12;

        /// <summary>How much of a hit Spite keeps, per level, and the ceiling on it.</summary>
        private const int SpiteCap = 60;

        /// <summary>How much further the deepest tier lets the bank fill.</summary>
        /// <remarks>
        /// It used to lift the ceiling away altogether, which is a different kind of
        /// problem from a high one: nothing stopped a player chipping themselves
        /// against something harmless until the bank held thousands and then handing
        /// all of it over in a single blow. Twice as much is a real reward and still a
        /// number.
        /// </remarks>
        private const int SpiteDeepCapMultiplier = 2;

        /// <summary>What share of a spite payout lands on the target's neighbours.</summary>
        private const float SpiteSplashShare = 0.5f;

        /// <summary>How far a splash reaches past the target, in tiles.</summary>
        private const int SpiteSplashRange = 1;

        /// <summary>What Last Stand counts as nearly dead, and what it pays.</summary>
        private const float LastStandThreshold = 1f / 3f;

        /// <summary>What share of a hit still gets through while braced.</summary>
        /// <remarks>
        /// Three quarters rather than three fifths. Last Stand's own tiers took its
        /// drawback away - braced against the damage, healing off every blow, and a
        /// free save once a floor - so the multiplier was no longer being paid for by
        /// standing anywhere dangerous.
        /// </remarks>
        private const float LastStandBracedShare = 0.75f;

        /// <summary>What each level of Last Stand multiplies a blow by while it is awake.</summary>
        /// <remarks>
        /// Half a level rather than a whole one: four levels was five times damage,
        /// and five times damage on a build that had also stopped being in danger is
        /// not a last stand, it is the best way to play. Three times at the deepest
        /// tier, which is still the largest single multiplier in the pool.
        /// </remarks>
        private const float LastStandPerLevel = 0.5f;

        /// <summary>What a blow landed while nearly dead gives back.</summary>
        private const int LastStandLeechHeal = 2;

        /// <summary>How many unhit blows Flurry will count.</summary>
        private const int FlurryMaxStreak = 10;

        /// <summary>What each blow of a Flurry adds, as a share of the weapon's damage.</summary>
        private const float FlurryStep = 0.06f;

        /// <summary>The same, with the weapon it was written for.</summary>
        private const float FlurryDaggerStep = 0.12f;

        /// <summary>How many unhit blows it will count once the run is lengthened.</summary>
        private const int FlurryLongMaxStreak = 16;

        /// <summary>How far a sweep reaches past the target, in tiles.</summary>
        private const int FlurrySweepRange = 1;

        /// <summary>What share of the blow a swept neighbour takes.</summary>
        private const float FlurrySweepShare = 0.6f;

        /// <summary>The most a run of blows will ever add, as a share of the weapon.</summary>
        /// <remarks>
        /// The level no longer multiplies this. It used to multiply a run that the
        /// level had also lengthened - sixteen blows times four levels times twelve
        /// per cent - which came to nearly eight times the weapon's own damage out of
        /// one keystone. The run is what earns the damage; what depth buys is the
        /// grace, the sweep, the longer run and the speed, which is enough for a tier
        /// to be worth taking without it also multiplying the whole total.
        /// </remarks>
        private const float FlurryBonusCap = 1.5f;

        /// <summary>How much faster the weapon swings at a full run of blows.</summary>
        private const float FlurryHasteMultiplier = 0.3f;

        /// <summary>How long that lasts before another blow has to renew it.</summary>
        private const int FlurryHasteDurationMs = 3000;

        /// <summary>The buff Flurry keeps on the player once it is swinging fast.</summary>
        private const string FlurryBuffId = "tylergibbs2.StardewRoguelike_Flurry";

        /// <summary>How hard a rock bursts for Prospector, per level.</summary>
        private const int ProspectorDamage = 30;

        /// <summary>How likely a broken rock gives up something extra, per level.</summary>
        private const double ProspectorBonusChance = 0.25;

        /// <summary>And the most likely it ever gets.</summary>
        private const double ProspectorBonusChanceCap = 0.6;

        /// <summary>How far a burst reaches, and how far it reaches once widened.</summary>
        private const int ProspectorBurstRadius = 1;

        private const int ProspectorWideBurstRadius = 2;

        /// <summary>
        /// Runs every keystone the player holds against a monster that has just died.
        /// </summary>
        /// <param name="location">Where it died.</param>
        /// <param name="monster">What died.</param>
        /// <param name="x">Its pixel position when it died.</param>
        /// <param name="y">The same.</param>
        public static void OnMonsterKilled(GameLocation location, Monster monster, int x, int y)
        {
            if (location is null || monster is null)
                return;

            Vector2 tile = new(x / 64, y / 64);

            // The floor's own difficulty, so a blast keeps pace with what it is
            // being thrown at rather than fading to nothing by the fortieth floor.
            float difficulty = location is StardewValley.Locations.MineShaft mine
                ? BossFloor.GetLevelDifficulty(mine)
                : 1f;

            Explode(location, tile, difficulty);
            Bomb(location, tile);
            Shoot(location, monster, x, y);
            Quicken();
        }

        /// <summary>Volatile: the corpse goes off.</summary>
        private static void Explode(GameLocation location, Vector2 tile, float difficulty)
        {
            int level = Perks.PerkLevel(Perks.PerkType.Volatile);
            if (level == 0)
                return;

            int limit = KeystoneTraits.Has(KeystoneTrait.ChainKills)
                ? BlastChainLimitChained
                : BlastChainLimit;

            if (Current.BlastDepth >= limit)
                return;

            if (Current.BlastDepth == 0)
                Current.BlastsThisCascade = 0;

            if (Current.BlastsThisCascade >= BlastBudget)
                return;

            Current.BlastsThisCascade++;

            int radius = BlastRadius;
            if (KeystoneTraits.Has(KeystoneTrait.WiderBlast))
                radius++;

            int damage = (int)(BlastDamage * level * difficulty);

            // A chain is the blast running out of room, not another blast.
            if (Current.BlastDepth > 0)
            {
                radius = Math.Max(1, radius / BlastChainFalloff);
                damage = Math.Max(1, damage / BlastChainFalloff);
            }

            // Counted around the call rather than after it. explode kills monsters
            // while it runs, and the kill hook that lands here runs with it, so the
            // depth has to be raised before anything inside can ask about it.
            Current.BlastDepth++;
            try
            {
                // Objects are left alone unless the player has earned otherwise: a
                // blast that broke the stone turns a crowd perk into a mining one.
                location.explode(
                    tile,
                    radius,
                    Game1.player,
                    damageFarmers: false,
                    damage_amount: damage,
                    destroyObjects: false);
            }
            finally
            {
                Current.BlastDepth--;
            }

            if (KeystoneTraits.Has(KeystoneTrait.BlastStuns))
                StunAround(location, tile, radius);

            // Reuses the ring Prospector breaks, so a blast and a burst take rock the
            // same way and spare a chest the same way.
            if (KeystoneTraits.Has(KeystoneTrait.BlastBreaksRock))
                BreakSurroundingStone(location, (int)tile.X, (int)tile.Y, Game1.player, radius);
        }

        /// <summary>Leaves whatever lived through a blast reeling.</summary>
        private static void StunAround(GameLocation location, Vector2 tile, int radius)
        {
            foreach (NPC character in location.characters)
            {
                if (character is not Monster monster || monster.Health <= 0)
                    continue;

                Vector2 at = monster.Tile;
                if (Math.Abs(at.X - tile.X) > radius || Math.Abs(at.Y - tile.Y) > radius)
                    continue;

                monster.stunTime.Value = Math.Max(monster.stunTime.Value, BlastStunMs);
            }
        }

        /// <summary>Bombardier: it sometimes leaves one behind.</summary>
        private static void Bomb(GameLocation location, Vector2 tile)
        {
            int level = Perks.PerkLevel(Perks.PerkType.Bombardier);
            if (level == 0 || Game1.random.NextDouble() >= BombChance * level)
                return;

            if (!location.CanItemBePlacedHere(tile))
                return;

            // A cherry bomb rather than a bomb: the player is standing next to this.
            StardewValley.Object bomb = new("286", 1)
            {
                TileLocation = tile,
                Fragility = 2
            };

            location.objects.Add(tile, bomb);
            bomb.performRemoveAction();
            location.playSound("thudStep");
        }

        /// <summary>Ricochet: the kill carries on to something else.</summary>
        private static void Shoot(GameLocation location, Monster killed, int x, int y)
        {
            int level = Perks.PerkLevel(Perks.PerkType.Ricochet);
            if (level == 0)
                return;

            foreach (Monster target in NearestMonsters(location, killed, x, y, level))
            {
                Point at = target.StandingPixel;
                Vector2 heading = new(at.X - x, at.Y - y);

                if (heading == Vector2.Zero)
                    continue;

                heading.Normalize();
                heading *= 16f;

                BasicProjectile shot = new(
                    (int)(killed.DamageToFarmer * level * 1.5f),
                    Projectile.shadowBall,
                    0, 0, 0f,
                    heading.X, heading.Y,
                    new Vector2(x - 32, y - 32),
                    "", null, "",
                    explode: false,
                    damagesMonsters: true,
                    location,
                    Game1.player);

                shot.ignoreTravelGracePeriod.Value = true;
                location.projectiles.Add(shot);
            }

            location.playSound("daggerswipe");
        }

        /// <summary>The monsters a ricochet will carry on to, nearest first.</summary>
        private static IEnumerable<Monster> NearestMonsters(GameLocation location, Monster killed, int x, int y, int howMany)
        {
            List<(Monster Monster, float Distance)> found = new();

            foreach (NPC character in location.characters)
            {
                if (character is not Monster other || other == killed || other.Health <= 0)
                    continue;

                Point at = other.StandingPixel;
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(at.X, at.Y));

                if (distance <= RicochetRange * 64)
                    found.Add((other, distance));
            }

            found.Sort((a, b) => a.Distance.CompareTo(b.Distance));

            for (int i = 0; i < Math.Min(howMany, found.Count); i++)
                yield return found[i].Monster;
        }

        /// <summary>The ceiling on momentum's stack, which one tier raises.</summary>
        private static int MomentumCeiling =>
            KeystoneTraits.Has(KeystoneTrait.DeeperStacks) ? MomentumDeepMaxStacks : MomentumMaxStacks;

        /// <summary>Momentum: killing begets killing, while it lasts.</summary>
        /// <param name="fromKill">
        /// Whether a monster died for this, rather than merely being hit. Only a kill
        /// pays out the sustain; otherwise a crowd would heal the player for standing
        /// in it and swinging.
        /// </param>
        private static void Quicken(bool fromKill = true)
        {
            int level = Perks.PerkLevel(Perks.PerkType.Momentum);
            if (level == 0)
                return;

            double now = Game1.currentGameTime?.TotalGameTime.TotalMilliseconds ?? 0;

            // The stack lapses on its own clock rather than with the buff, so a buff
            // cleared by something else cannot leave the count high.
            if (now - Current.MomentumRenewed > MomentumDurationMs)
                Current.MomentumStacks = 0;

            bool wasFull = Current.MomentumStacks >= MomentumCeiling;

            Current.MomentumStacks = Math.Min(Current.MomentumStacks + 1, MomentumCeiling);
            Current.MomentumRenewed = now;

            // Paid at the top of the ramp rather than along it, so the health is what
            // holding momentum is worth rather than a trickle for fighting at all.
            if (fromKill && wasFull && KeystoneTraits.Has(KeystoneTrait.StackedSustain))
                Sustain.Heal(MomentumSustainHeal);

            ApplyMomentumBuff(level);
        }

        /// <summary>Puts the stack the player is on onto them as a buff.</summary>
        /// <remarks>
        /// Its own method because two things set it now: a kill or a landed blow
        /// raising the stack, and the tick that holds the stack open while something
        /// is still near. Both have to write the same buff or the one that did not
        /// would let it expire underneath the other.
        /// </remarks>
        private static void ApplyMomentumBuff(int level)
        {
            if (level == 0)
                return;

            BuffEffects effects = new();
            effects.Speed.Value = Math.Min(Current.MomentumStacks * level, MomentumSpeedCap);
            effects.Attack.Value = Math.Min(Current.MomentumStacks * level, MomentumAttackCap);

            Game1.player.applyBuff(new Buff(
                id: MomentumBuffId,
                source: "Roguelike",
                displaySource: Perks.GetPerkDisplayName(Perks.PerkType.Momentum),
                duration: MomentumDurationMs,
                iconTexture: Perks.GetPerkIconTexture(Perks.PerkType.Momentum),
                iconSheetIndex: 395,
                effects: effects));
        }

        /// <summary>
        /// Banks what the player just took, and ends their run of untouched blows.
        /// </summary>
        /// <param name="damage">What got through, after everything that reduces it.</param>
        /// <param name="blocked">What armour and the rest turned away before it did.</param>
        public static void OnPlayerHurt(int damage, int blocked)
        {
            // Flurry is a run of blows without being hit. This is being hit.
            EndStreak();

            int spite = Perks.PerkLevel(Perks.PerkType.Spite);
            if (spite == 0)
                return;

            // Armour used to make Spite worse: it banked only what got through, so
            // every point of defence in the pool was a point off this keystone. What
            // was stopped is banked too now, and the two stop working against each
            // other.
            int taken = KeystoneTraits.Has(KeystoneTrait.BankBlocked)
                ? damage + blocked
                : damage;

            if (taken <= 0)
                return;

            Current.Banked += taken * spite;

            int cap = SpiteCap * spite;
            if (KeystoneTraits.Has(KeystoneTrait.BiggerBank))
                cap *= SpiteDeepCapMultiplier;

            Current.Banked = Math.Min(Current.Banked, cap);
        }

        /// <summary>The ceiling on a run of blows, which one tier raises.</summary>
        private static int FlurryCeiling =>
            KeystoneTraits.Has(KeystoneTrait.LongerStreak) ? FlurryLongMaxStreak : FlurryMaxStreak;

        /// <summary>Ends a run of untouched blows, unless this one is forgiven.</summary>
        /// <remarks>
        /// One hit used to erase ten blows of work, which made the keystone
        /// all-or-nothing on exactly the crowded floors its own dagger bias pushes the
        /// player onto: there is no way to stand in a crowd and never be touched. The
        /// grace is spent per run of blows rather than per floor, so it forgives the
        /// unlucky hit without forgiving standing still and being hit repeatedly.
        /// </remarks>
        private static void EndStreak()
        {
            if (Current.Streak > 0 && !Current.GraceSpent && KeystoneTraits.Has(KeystoneTrait.StreakGrace))
            {
                Current.GraceSpent = true;
                return;
            }

            Current.Streak = 0;
            Current.GraceSpent = false;
        }

        /// <summary>
        /// Flurry at a full run: the swing reaches what stands alongside the target.
        /// </summary>
        private static void SweepFlurry(GameLocation location, Rectangle area, int minDamage, int maxDamage)
        {
            if (Current.Spreading || !KeystoneTraits.Has(KeystoneTrait.FlurrySweep))
                return;

            if (Current.Streak < FlurryCeiling)
                return;

            int swept = Math.Max(1, (int)(maxDamage * FlurrySweepShare));

            HitNeighbours(location, area, FlurrySweepRange, swept, swept);
        }

        /// <summary>Flurry at a full run: the weapon swings faster for it.</summary>
        private static void ApplyFlurryHaste()
        {
            if (!KeystoneTraits.Has(KeystoneTrait.FlurryHaste) || Current.Streak < FlurryCeiling)
                return;

            BuffEffects effects = new();
            effects.WeaponSpeedMultiplier.Value = FlurryHasteMultiplier;

            Game1.player.applyBuff(new Buff(
                id: FlurryBuffId,
                source: "Roguelike",
                displaySource: Perks.GetPerkDisplayName(Perks.PerkType.Flurry),
                duration: FlurryHasteDurationMs,
                iconTexture: Perks.GetPerkIconTexture(Perks.PerkType.Flurry),
                iconSheetIndex: 0,
                effects: effects));
        }

        /// <summary>
        /// Whether the blow being resolved is one a keystone caused rather than struck.
        /// </summary>
        /// <remarks>
        /// Read by the damage postfix, which does more than call into here: the Leech
        /// perk and the Brittle Crown curse live beside that call. Guarding only this
        /// class left those two multiplying with every sweep and splash - five
        /// neighbours meant five leech rolls and five gold payouts off one swing -
        /// which is the same mistake as the sustain one, made inside the fix for it.
        /// </remarks>
        public static bool IsSpreading => Current.Spreading;

        /// <summary>
        /// Puts a share of what spite just handed back onto the target's neighbours.
        /// </summary>
        /// <remarks>
        /// Run after the blow rather than inside it. The payout is decided while the
        /// damage numbers are being worked out, which is before anyone knows whether
        /// the blow landed at all, and a splash from a swing that missed would be
        /// damage out of nothing.
        /// </remarks>
        private static void SplashSpite(GameLocation location, Rectangle area)
        {
            int paid = Current.SpitePaid;
            Current.SpitePaid = 0;

            if (paid <= 0 || Current.Spreading || !KeystoneTraits.Has(KeystoneTrait.SpiteSplash))
                return;

            int splash = Math.Max(1, (int)(paid * SpiteSplashShare));

            HitNeighbours(location, area, SpiteSplashRange, splash, splash);
        }

        /// <summary>
        /// Hits whatever stands near <paramref name="area"/> without standing in it.
        /// </summary>
        /// <remarks>
        /// Shared by the splash and the sweep. Each neighbour is struck on its own
        /// bounding box rather than the whole widened area being struck at once,
        /// because one wide blow would land on the original target a second time and
        /// read as the keystone quietly doubling the player's damage.
        ///
        /// Guarded, because damageMonster is the method this is called from the middle
        /// of: a sweep that swept would spread across the floor a neighbour at a time.
        /// </remarks>
        private static void HitNeighbours(GameLocation location, Rectangle area, int range, int minDamage, int maxDamage)
        {
            Rectangle widened = new(
                area.X - range * 64,
                area.Y - range * 64,
                area.Width + range * 128,
                area.Height + range * 128);

            List<Monster> neighbours = new();

            foreach (NPC character in location.characters)
            {
                if (character is not Monster monster || monster.Health <= 0)
                    continue;

                Rectangle box = monster.GetBoundingBox();

                if (box.Intersects(area) || !box.Intersects(widened))
                    continue;

                neighbours.Add(monster);
            }

            if (neighbours.Count == 0)
                return;

            Current.Spreading = true;
            try
            {
                foreach (Monster monster in neighbours)
                {
                    location.damageMonster(
                        monster.GetBoundingBox(), minDamage, maxDamage, false, Game1.player);
                }
            }
            finally
            {
                Current.Spreading = false;
            }
        }

        /// <summary>Whether the player is hurt enough for Last Stand to be awake.</summary>
        private static bool NearlyDead =>
            Game1.player.health <= Game1.player.maxHealth * LastStandThreshold;

        /// <summary>Takes the edge off a hit while the player is nearly dead.</summary>
        /// <remarks>
        /// Last Stand paid only for dying: more damage the closer to death, and
        /// nothing whatever to help the player stop dying. That is a spiral rather
        /// than a comeback - the keystone was at its strongest in exactly the moment
        /// it was least likely to matter. This and the leech are what turn the
        /// threshold into somewhere a build can live rather than somewhere it ends.
        /// </remarks>
        public static void ModifyIncomingDamage(ref int damage)
        {
            if (Perks.PerkLevel(Perks.PerkType.LastStand) == 0)
                return;

            if (!KeystoneTraits.Has(KeystoneTrait.Braced) || !NearlyDead)
                return;

            damage = Math.Max(1, (int)Math.Round(damage * LastStandBracedShare));
        }

        /// <summary>
        /// Takes a killing blow down to a survivable one, once a floor.
        /// </summary>
        /// <remarks>
        /// The blow is trimmed rather than the health written afterwards, and the
        /// caller carries on as though the player had simply been hit hard. Setting
        /// the health directly and returning early looked tidier and was wrong: the
        /// invincible window, the knockback and the hit sound are all further down
        /// the same method, so a player saved that way stood there with no mercy
        /// frames and died to the next contact tick instead. Spent per floor rather
        /// than per run, because a floor is the unit the player is already weighing
        /// when they decide whether to push on.
        /// </remarks>
        public static bool TrySecondWind(ref int damage)
        {
            if (Perks.PerkLevel(Perks.PerkType.LastStand) == 0)
                return false;

            if (Current.SecondWindSpent || !KeystoneTraits.Has(KeystoneTrait.SecondWind))
                return false;

            if (Game1.player.health - damage > 0)
                return false;

            Current.SecondWindSpent = true;

            // Leaves exactly one health, whatever the blow was for.
            damage = Math.Max(0, Game1.player.health - 1);

            Game1.playSound("yoba");
            Game1.player.startGlowing(Color.Gold, border: false, 0.4f);
            DelayedAction.functionAfterDelay(Game1.player.stopGlowing, 1500);

            Game1.chatBox?.addMessage(I18n.Keystone_SecondWind(), Color.Gold);

            return true;
        }

        /// <summary>Hands back what is spent per floor. Called on arriving at one.</summary>
        /// <remarks>
        /// Spite's bank is emptied here because nothing else ever emptied it. It is
        /// documented as the damage taken since the last blow, but the only thing
        /// that cleared it was spending it, and a run lasts forty floors - so a
        /// player could stand in front of something harmless on a quiet floor,
        /// chip themselves to the ceiling, walk to the boss and open the fight with
        /// the whole bank. Every other keystone already lapses on its own: a streak
        /// ends on being hit, a momentum stack times out. This one had no clock at
        /// all, which made the ceiling a target to be filled at leisure rather than
        /// a bound on what one fight could give back.
        ///
        /// The payout the splash follows goes with it. It is read on the next blow
        /// that lands, and a swing that paid the bank and then missed left it set,
        /// so the first hit on the new floor would splash the last floor's bank.
        /// </remarks>
        public static void OnFloorEntered()
        {
            Current.SecondWindSpent = false;
            Current.Banked = 0;
            Current.SpitePaid = 0;
        }

        /// <summary>Counts a blow that landed, for Flurry.</summary>
        public static void OnMonsterHit(GameLocation? location, Rectangle area, int minDamage, int maxDamage)
        {
            // A blow this keystone set off is not a blow the player struck. Counting
            // it would let a sweep lengthen the run that caused the sweep, and let a
            // splash stack momentum it did not earn.
            if (Current.Spreading)
                return;

            if (Perks.HasPerk(Perks.PerkType.Flurry))
                Current.Streak = Math.Min(Current.Streak + 1, FlurryCeiling);

            // Momentum off a landed blow, not only off a kill. Without this the
            // keystone is switched off on exactly the floor a build matters most on:
            // a boss floor has one monster, so there is nothing to kill until it is
            // over and nothing to stack from while it is not.
            if (KeystoneTraits.Has(KeystoneTrait.StackOnHit))
                Quicken(fromKill: false);

            if (Perks.PerkLevel(Perks.PerkType.LastStand) > 0
                && KeystoneTraits.Has(KeystoneTrait.Leeching)
                && NearlyDead)
                Sustain.Heal(LastStandLeechHeal);

            if (location is null)
                return;

            SplashSpite(location, area);
            SweepFlurry(location, area, minDamage, maxDamage);
            ApplyFlurryHaste();
        }

        /// <summary>
        /// Keeps momentum open while there is still something to fight.
        /// </summary>
        /// <remarks>
        /// Called every tick rather than off a kill, because what this tier changes is
        /// what happens when the player is *not* killing anything - swinging at
        /// something armoured, or crossing a room to the next monster. The stack is
        /// renewed rather than frozen, so it still lapses the moment the floor is
        /// clear and nothing is near.
        /// </remarks>
        public static void Update()
        {
            if (!KeystoneTraits.Has(KeystoneTrait.HeldStacks) || Current.MomentumStacks == 0)
                return;

            if (!Context.IsWorldReady || Game1.currentLocation is not GameLocation location)
                return;

            // Twice a second is plenty to keep a four second buff alive, and spares
            // the sweep over every character on the floor on the other fifty-eight
            // ticks.
            if (++Current.HoldTicks < HoldCheckInterval)
                return;

            Current.HoldTicks = 0;

            Vector2 player = Game1.player.Tile;

            foreach (NPC character in location.characters)
            {
                if (character is not Monster monster || monster.Health <= 0)
                    continue;

                if (Math.Abs(monster.Tile.X - player.X) > MomentumHoldRange
                    || Math.Abs(monster.Tile.Y - player.Y) > MomentumHoldRange)
                    continue;

                Current.MomentumRenewed = Game1.currentGameTime?.TotalGameTime.TotalMilliseconds ?? 0;
                ApplyMomentumBuff(Perks.PerkLevel(Perks.PerkType.Momentum));
                return;
            }
        }

        /// <summary>
        /// Puts the keystones that answer to the player's own state into a blow.
        /// </summary>
        /// <remarks>
        /// Three of them meet here because all three change what a swing is worth
        /// rather than what happens after it: what the player has taken, how long
        /// they have gone untouched, and how close they are to dying.
        /// </remarks>
        public static void ModifyOutgoingDamage(ref int minDamage, ref int maxDamage)
        {
            // Taken before anything touches it. Flurry's bonus is written as a share
            // of the weapon, and it was reading this after Last Stand had already
            // tripled it - so two keystones that each looked bounded compounded into
            // one that was not.
            int weaponMax = maxDamage;

            // Last Stand multiplies, so it is applied to the weapon's own numbers
            // before anything is added to them.
            int lastStand = Perks.PerkLevel(Perks.PerkType.LastStand);
            if (lastStand > 0 && Game1.player.health <= Game1.player.maxHealth * LastStandThreshold)
            {
                float multiplier = 1f + lastStand * LastStandPerLevel;
                minDamage = (int)(minDamage * multiplier);
                maxDamage = (int)(maxDamage * multiplier);
            }

            int bonus = 0;

            // Spite hands back everything taken since the last blow, once.
            if (Current.Banked > 0)
            {
                bonus += Current.Banked;
                Current.SpitePaid = Current.Banked;
                Current.Banked = 0;
            }

            int flurry = Perks.PerkLevel(Perks.PerkType.Flurry);
            if (flurry > 0 && Current.Streak > 0)
            {
                // Worth twice as much in the hand it was written for, which is what
                // makes the weapon part of the build rather than whatever was found
                // most recently.
                float step = HoldingDagger() ? FlurryDaggerStep : FlurryStep;
                float share = Math.Min(step * Current.Streak, FlurryBonusCap);
                bonus += (int)(weaponMax * share);
            }

            minDamage += bonus;
            maxDamage += bonus;
        }

        private static bool HoldingDagger()
        {
            return Game1.player.CurrentTool is MeleeWeapon weapon
                   && weapon.type.Value == MeleeWeapon.dagger;
        }

        /// <summary>Prospector: a broken rock goes off, and gives up more of what was in it.</summary>
        public static void OnStoneBroken(GameLocation location, int x, int y, Farmer who)
        {
            if (!Burst(location, x, y, who))
                return;

            int level = Perks.PerkLevel(Perks.PerkType.Prospector);

            // Clamped, because a quarter a level reaches exactly one at the fourth -
            // a certainty dressed as a roll, and a sign the coefficient and the
            // ceiling were never set with each other in mind.
            double bonusChance = Math.Min(ProspectorBonusChance * level, ProspectorBonusChanceCap);

            if (Game1.random.NextDouble() < bonusChance)
                Game1.createMultipleObjectDebris("382", x, y, 1, who.UniqueMultiplayerID, location);
        }

        /// <summary>
        /// Prospector <see cref="KeystoneTrait.BreakBoulders"/>: a boulder goes off too.
        /// </summary>
        /// <remarks>
        /// Boulders are not rock as far as the game is concerned - they are terrain
        /// features rather than objects, broken through a different path entirely -
        /// so a keystone about breaking rock walked straight past the biggest rock in
        /// the mine until this tier. It has somewhere obvious to matter: a speedrun
        /// floor lays out nine of them.
        /// </remarks>
        public static void OnBoulderBroken(GameLocation location, int x, int y, Farmer who)
        {
            if (!KeystoneTraits.Has(KeystoneTrait.BreakBoulders))
                return;

            Burst(location, x, y, who);
        }

        /// <summary>The burst itself. Returns whether one actually went off.</summary>
        /// <remarks>
        /// Shared by the two things that set it off, so a tier that widens the burst
        /// widens it for rock and boulders at once rather than for whichever of them
        /// was remembered.
        ///
        /// The blast spares objects. It is deliberately not what breaks the
        /// surrounding rock: explode would take a chest with it, and the vault floors
        /// put chests on the floor beside everything else.
        /// </remarks>
        private static bool Burst(GameLocation location, int x, int y, Farmer who)
        {
            int level = Perks.PerkLevel(Perks.PerkType.Prospector);
            if (level == 0 || who != Game1.player)
                return false;

            // A rock taken by a burst does not burst in turn. Without this the ring
            // goes off once per rock in it, which is nine times the damage out of one
            // swing and a chain with nothing to stop it short of the whole floor.
            if (Current.Bursting)
                return false;

            int radius = KeystoneTraits.Has(KeystoneTrait.WiderBurst)
                ? ProspectorWideBurstRadius
                : ProspectorBurstRadius;

            location.explode(
                new Vector2(x, y),
                radius,
                who,
                damageFarmers: false,
                damage_amount: ProspectorDamage * level,
                destroyObjects: false);

            if (KeystoneTraits.Has(KeystoneTrait.BreakAdjacent))
                BreakSurroundingStone(location, x, y, who, radius);

            return true;
        }

        /// <summary>Breaks the rock a burst reached, for what is in it.</summary>
        /// <remarks>
        /// Each one goes through OnStoneDestroyed, which is the path the pickaxe
        /// itself uses, so a rock taken this way drops what it would have dropped and
        /// counts where it would have counted - the quarry floor's score among them.
        ///
        /// IsBreakableStone is the whole test, and a safe one: every ore and gem node
        /// in the game is named Stone in category -999, so it covers the ore a
        /// prospector is here for while leaving chests, barrels and anything a player
        /// set down alone.
        /// </remarks>
        private static void BreakSurroundingStone(GameLocation location, int x, int y, Farmer who, int radius)
        {
            // Collected before any of it is broken, because breaking mutates the
            // collection being walked.
            List<Vector2> reached = new();

            for (int i = -radius; i <= radius; i++)
            {
                for (int j = -radius; j <= radius; j++)
                {
                    if (i == 0 && j == 0)
                        continue;

                    Vector2 tile = new(x + i, y + j);

                    if (location.Objects.TryGetValue(tile, out SObject? neighbour) && neighbour.IsBreakableStone())
                        reached.Add(tile);
                }
            }

            if (reached.Count == 0)
                return;

            Current.Bursting = true;
            try
            {
                foreach (Vector2 tile in reached)
                {
                    if (!location.Objects.TryGetValue(tile, out SObject? neighbour))
                        continue;

                    // The pickaxe's own order: the drops are worked out while the rock
                    // is still on the map, then it is taken off.
                    location.OnStoneDestroyed(neighbour.ItemId, (int)tile.X, (int)tile.Y, who);

                    neighbour.performRemoveAction();
                    location.Objects.Remove(tile);

                    Game1.createRadialDebris(
                        location, 14, (int)tile.X, (int)tile.Y, Game1.random.Next(2, 5), resource: false);
                }

                location.playSound("stoneCrack");
            }
            finally
            {
                Current.Bursting = false;
            }
        }

        /// <summary>Forgets what the run was carrying. Called when a run begins.</summary>
        /// <remarks>
        /// A new state rather than a field-by-field clearing of the old one, which is
        /// the reason the state is one object: the hand-written version had to be
        /// remembered every time a keystone gained a field, and twice it was not -
        /// the burst guard and momentum's hold counter were both carried into the
        /// next run. A guard stuck true survives into a run that then has no bursts
        /// at all.
        /// </remarks>
        public static void Reset()
        {
            ScreenState.Value = new();
        }
    }
}

using Microsoft.Xna.Framework;
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
        /// <summary>How far Volatile's blast reaches, in tiles, at one level.</summary>
        private const int BlastRadius = 2;

        /// <summary>What Volatile's blast hits for, before the floor's difficulty.</summary>
        private const int BlastDamage = 25;

        /// <summary>How long Momentum's stack lasts, and how high it stacks.</summary>
        private const int MomentumDurationMs = 4000;
        private const int MomentumMaxStacks = 5;

        /// <summary>The buff Momentum keeps on the player.</summary>
        private const string MomentumBuffId = "tylergibbs2.StardewRoguelike_Momentum";

        /// <summary>How likely Bombardier is to leave a bomb, per level.</summary>
        private const double BombChance = 0.15;

        /// <summary>How far Ricochet looks for something else to hit, in tiles.</summary>
        private const int RicochetRange = 12;

        /// <summary>How much of a hit Spite keeps, per level, and the ceiling on it.</summary>
        private const int SpiteCap = 60;

        /// <summary>What Last Stand counts as nearly dead, and what it pays.</summary>
        private const float LastStandThreshold = 1f / 3f;

        /// <summary>How many unhit blows Flurry will count.</summary>
        private const int FlurryMaxStreak = 10;

        /// <summary>What each blow of a Flurry adds, as a share of the weapon's damage.</summary>
        private const float FlurryStep = 0.06f;

        /// <summary>The same, with the weapon it was written for.</summary>
        private const float FlurryDaggerStep = 0.12f;

        /// <summary>How hard a rock bursts for Prospector, per level.</summary>
        private const int ProspectorDamage = 30;

        /// <summary>How likely a broken rock gives up something extra, per level.</summary>
        private const double ProspectorBonusChance = 0.25;

        /// <summary>Damage taken since the last blow, waiting to be handed back.</summary>
        private static int banked;

        /// <summary>Blows landed since the player was last touched.</summary>
        private static int streak;

        /// <summary>Momentum's stack count, which decays with the buff.</summary>
        private static int momentumStacks;

        /// <summary>When that stack was last added, so it can lapse.</summary>
        private static double momentumRenewed;

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

            // Objects are left alone: a blast that broke the stone would turn this
            // into a mining perk, and it is meant to be a crowd perk.
            location.explode(
                tile,
                BlastRadius + level - 1,
                Game1.player,
                damageFarmers: false,
                damage_amount: (int)(BlastDamage * level * difficulty),
                destroyObjects: false);
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

        /// <summary>Momentum: killing begets killing, while it lasts.</summary>
        private static void Quicken()
        {
            int level = Perks.PerkLevel(Perks.PerkType.Momentum);
            if (level == 0)
                return;

            double now = Game1.currentGameTime?.TotalGameTime.TotalMilliseconds ?? 0;

            // The stack lapses on its own clock rather than with the buff, so a buff
            // cleared by something else cannot leave the count high.
            if (now - momentumRenewed > MomentumDurationMs)
                momentumStacks = 0;

            momentumStacks = Math.Min(momentumStacks + 1, MomentumMaxStacks);
            momentumRenewed = now;

            BuffEffects effects = new();
            effects.Speed.Value = momentumStacks * level;
            effects.Attack.Value = momentumStacks * level;

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
        public static void OnPlayerHurt(int damage)
        {
            // Flurry is a run of blows without being hit. This is being hit.
            streak = 0;

            int spite = Perks.PerkLevel(Perks.PerkType.Spite);
            if (spite == 0 || damage <= 0)
                return;

            banked = Math.Min(banked + damage * spite, SpiteCap * spite);
        }

        /// <summary>Counts a blow that landed, for Flurry.</summary>
        public static void OnMonsterHit()
        {
            if (Perks.HasPerk(Perks.PerkType.Flurry))
                streak = Math.Min(streak + 1, FlurryMaxStreak);
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
            // Last Stand multiplies, so it is applied to the weapon's own numbers
            // before anything is added to them.
            int lastStand = Perks.PerkLevel(Perks.PerkType.LastStand);
            if (lastStand > 0 && Game1.player.health <= Game1.player.maxHealth * LastStandThreshold)
            {
                minDamage = (int)(minDamage * (1 + lastStand));
                maxDamage = (int)(maxDamage * (1 + lastStand));
            }

            int bonus = 0;

            // Spite hands back everything taken since the last blow, once.
            if (banked > 0)
            {
                bonus += banked;
                banked = 0;
            }

            int flurry = Perks.PerkLevel(Perks.PerkType.Flurry);
            if (flurry > 0 && streak > 0)
            {
                // Worth twice as much in the hand it was written for, which is what
                // makes the weapon part of the build rather than whatever was found
                // most recently.
                float step = HoldingDagger() ? FlurryDaggerStep : FlurryStep;
                bonus += (int)(maxDamage * step * streak * flurry);
            }

            minDamage += bonus;
            maxDamage += bonus;
        }

        private static bool HoldingDagger()
        {
            return Game1.player.CurrentTool is MeleeWeapon weapon
                   && weapon.type.Value == MeleeWeapon.dagger;
        }

        /// <summary>
        /// Prospector: the rock goes off, and gives up more of what was in it.
        /// </summary>
        public static void OnStoneBroken(GameLocation location, int x, int y, Farmer who)
        {
            int level = Perks.PerkLevel(Perks.PerkType.Prospector);
            if (level == 0 || who != Game1.player)
                return;

            // Objects are spared: a burst that broke the rock beside it would clear a
            // floor on its own, and would take a chest with it on a vault floor.
            location.explode(
                new Vector2(x, y),
                1,
                who,
                damageFarmers: false,
                damage_amount: ProspectorDamage * level,
                destroyObjects: false);

            if (Game1.random.NextDouble() < ProspectorBonusChance * level)
                Game1.createMultipleObjectDebris("382", x, y, 1, who.UniqueMultiplayerID, location);
        }

        /// <summary>Forgets what the run was carrying. Called when a run begins.</summary>
        public static void Reset()
        {
            momentumStacks = 0;
            momentumRenewed = 0;
            banked = 0;
            streak = 0;
        }
    }
}

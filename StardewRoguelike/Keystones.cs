using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buffs;
using StardewValley.Monsters;
using StardewValley.Projectiles;
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

        /// <summary>Forgets the run's momentum. Called when a run begins.</summary>
        public static void Reset()
        {
            momentumStacks = 0;
            momentumRenewed = 0;
        }
    }
}

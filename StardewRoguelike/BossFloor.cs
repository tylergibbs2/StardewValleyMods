using StardewRoguelike.Bosses;
using StardewRoguelike.VirtualProperties;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Monsters;
using System;
using System.Collections.Generic;

namespace StardewRoguelike
{
    public static class BossFloor
    {
        /// <summary>
        /// Determines whether a specified MineShaft is a boss floor.
        /// </summary>
        /// <param name="mine">The MineShaft.</param>
        /// <returns>true if it is, false otherwise</returns>
        public static bool IsBossFloor(MineShaft mine)
        {
            return IsBossFloor(Roguelike.GetLevelFromMineshaft(mine));
        }

        /// <summary>
        /// Determines whether a level is a boss floor.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <returns>true if it is, false otherwise</returns>
        public static bool IsBossFloor(int level)
        {
            if (DebugCommands.ForcedBossIndex != -1)
                return true;

            return Merchant.IsMerchantFloor(level + 1);
        }

        /// <summary>
        /// Gets the index (which boss) is at a specified floor.
        /// </summary>
        /// <param name="floor">The floor.</param>
        /// <returns>The index of the boss that is at that floor.</returns>
        public static int GetBossIndexForFloor(int floor)
        {
            if (DebugCommands.ForcedBossIndex != -1)
                return DebugCommands.ForcedBossIndex;

            return floor % 48 / 6;
        }

        /// <summary>
        /// Calculates the difficulty of any specified MineShaft.
        /// </summary>
        /// <param name="mine">The MineShaft.</param>
        /// <returns>The floor's difficulty.</returns>
        public static float GetLevelDifficulty(MineShaft mine)
        {
            return GetLevelDifficulty(Roguelike.GetLevelFromMineshaft(mine));
        }

        /// <summary>
        /// Calculates the difficulty of any specified floor.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <returns>The floor's difficulty.</returns>
        /// <summary>The most hard mode ever multiplies a floor's difficulty by.</summary>
        private const float HardModeCeiling = 0.25f;

        /// <summary>
        /// What hard mode is worth on <paramref name="level"/>, as a multiplier.
        /// </summary>
        /// <remarks>
        /// It used to be a quarter added to the difficulty, which is not a quarter of
        /// anything: the number it was added to grows all run, so the same constant
        /// was worth a fifth more boss health on the fifth floor and a twelfth more on
        /// the forty-seventh. Hard mode bit hardest at the start and faded, which is
        /// backwards for the mode meant to be the harder one - and the early floors
        /// are where it hurt most, since a slime with a fifth more health is fought
        /// with whatever the first shop sold.
        ///
        /// A multiplier that climbs with the floor instead, so hard mode is worth
        /// almost nothing at the first boss and a full quarter by the last. The early
        /// floors are still harder than normal's - the extra volleys, the extra bees,
        /// the darker mineshafts and the quarter more damage every monster deals are
        /// untouched by this - they are just no longer harder by way of a health bar.
        ///
        /// Capped at the end of a first pass. Past that the loop has an escalation of
        /// its own and hard mode stays a flat quarter on top of it, rather than the
        /// two compounding without a ceiling.
        /// </remarks>
        private static float HardModeScale(int level)
        {
            if (!Roguelike.HardMode)
                return 1f;

            float through = Math.Min(1f, level / (float)Constants.ScalingOrder[^1]);

            return 1f + HardModeCeiling * through;
        }

        public static float GetLevelDifficulty(int level)
        {
            if (DebugCommands.ForcedDifficulty != 0f)
                return DebugCommands.ForcedDifficulty;

            float difficulty = (float)Math.Round(level / (float)Constants.ScalingOrder[^1], 2) + 1f;

            if (level > Constants.ScalingOrder[^1])
            {
                int levelsPostLoop = level - Constants.ScalingOrder[^1];
                difficulty += 0.25f * (levelsPostLoop / 6f);
            }

            return difficulty * HardModeScale(level);
        }

        /// <summary>How fast boss health climbs; see <see cref="GetHealthDifficulty"/>.</summary>
        /// <remarks>
        /// Half the divisor the general difficulty uses, so health rises at twice the
        /// rate of everything else that scales with the floor.
        /// </remarks>
        private const float HealthScaleDivisor = 24f;

        /// <summary>
        /// How much health a boss carries over its base, for the floor it stands on.
        /// </summary>
        /// <remarks>
        /// Health is on a steeper curve than the rest of the difficulty, because it is
        /// the only part that has to keep pace with the player rather than with the
        /// floor number. A run's damage multiplies from several directions at once -
        /// the weapon alone goes from three to ninety over a first pass, and the
        /// professions, the hat, the forge, the keystone and the crit perks all stack
        /// on top of it - while <see cref="GetLevelDifficulty"/> does no more than
        /// double across the same stretch. Measured on that curve, bosses died faster
        /// the further a run got: the eighth took a third as long to kill as the
        /// first.
        ///
        /// Damage deliberately stays on the general curve. Raising what a boss hits
        /// for alongside its health would make the late floors quicker to lose and
        /// slower to win, which is the opposite of the point - a boss should take
        /// longer to kill, not be faster at killing you.
        /// </remarks>
        public static float GetHealthDifficulty(int level)
        {
            if (DebugCommands.ForcedDifficulty != 0f)
                return DebugCommands.ForcedDifficulty;

            float difficulty = (float)Math.Round(level / HealthScaleDivisor, 2) + 1f;

            // The loop is left on the rate it already had. It is unmeasured ground and
            // the first pass is what this was written against.
            if (level > Constants.ScalingOrder[^1])
            {
                int levelsPostLoop = level - Constants.ScalingOrder[^1];
                difficulty += 0.25f * (levelsPostLoop / 6f);
            }

            return difficulty * HardModeScale(level);
        }

        /// <summary>The same, for a mine rather than a level.</summary>
        public static float GetHealthDifficulty(MineShaft mine)
        {
            return GetHealthDifficulty(Roguelike.GetLevelFromMineshaft(mine));
        }

        public static void Setup(MineShaft mine)
        {
            var spawnedBoss = SpawnBoss(mine);

            if (spawnedBoss == typeof(Modulosaurus))
                mine.get_MineShaftCustomDestination().Value = new(24, 10);
        }

        /// <summary>
        /// Spawns a boss in a specified MineShaft.
        /// The boss is instantiated with the proper difficulty.
        /// </summary>
        /// <param name="mine">The mine.</param>
        public static Type SpawnBoss(MineShaft mine)
        {
            Type bossType;
            int level = Roguelike.GetLevelFromMineshaft(mine);

            if (DebugCommands.ForcedBossIndex != -1)
            {
                var bosses = BossManager.GetFlattenedBosses();
                bossType = bosses[DebugCommands.ForcedBossIndex];
            }
            else
            {
                int index = GetBossIndexForFloor(level);

                var bossOptions = BossManager.mainBossTypes[index];
                bossType = bossOptions[Roguelike.FloorRng.Next(bossOptions.Count)];
            }

            float difficulty = GetLevelDifficulty(mine);

            if (bossType == typeof(TutorialSlime) && level > Constants.ScalingOrder[^1])
                bossType = typeof(LoopedSlime);

            Monster boss = (Monster)Activator.CreateInstance(bossType, new object[] { difficulty })!;
            boss.MaxHealth = (int)(BossManager.GetBaseHealth(mine, bossType) * GetHealthDifficulty(mine));
            boss.Health = boss.MaxHealth;
            boss.DamageToFarmer = (int)(BossManager.GetBaseDamageToFarmer(mine, bossType) * difficulty);
            mine.characters.Add(boss);

            return bossType;
        }

        /// <summary>
        /// Spawns a specified boss in a specified MineShaft.
        /// The boss is instantiated with the proper difficulty.
        /// </summary>
        /// <param name="mine">The mine.</param>
        public static void SpawnBoss(MineShaft mine, Type whichBoss)
        {
            float difficulty = GetLevelDifficulty(mine);

            Monster boss = (Monster)Activator.CreateInstance(whichBoss, new object[] { difficulty })!;
            boss.MaxHealth = (int)(BossManager.GetBaseHealth(mine, whichBoss) * GetHealthDifficulty(mine));
            boss.Health = boss.MaxHealth;
            boss.DamageToFarmer = (int)(BossManager.GetBaseDamageToFarmer(mine, whichBoss) * difficulty);
            mine.characters.Add(boss);
        }

        /// <summary>
        /// Gets the map path to load for the specified <see cref="GameLocation"/>
        /// </summary>
        /// <param name="location">The location.</param>
        /// <returns>The map path to load.</returns>
        public static string GetMapPath(GameLocation location)
        {
            foreach (NPC character in location.characters)
            {
                if (character is IBossMonster boss)
                    return boss.MapPath;
            }

            return "";
        }

        /// <summary>
        /// Gets all music tracks for a specified <see cref="GameLocation"/>
        /// </summary>
        /// <param name="location">The location.</param>
        /// <returns>List of music tracks that can play.</returns>
        public static List<string> GetMusicTracks(GameLocation location)
        {
            foreach (NPC character in location.characters)
            {
                if (character is IBossMonster boss)
                    return boss.MusicTracks;
            }

            return new() { "none" };
        }
    }
}

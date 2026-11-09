using Microsoft.Xna.Framework;
using StardewValley.Locations;
using StardewValley.Monsters;
using System;
using System.Collections.Generic;

namespace StardewRoguelike
{
    /// <summary>
    /// Which of the mine's monsters are fit for which job.
    ///
    /// The floors ask the game for "a monster for this depth" and then assume the
    /// thing they get back will come and fight. Several of them will not, and the
    /// assumption is invisible until a floor that depends on it quietly stops
    /// working.
    /// </summary>
    internal static class MonsterRoster
    {
        /// <summary>The monsters that will actually come to the player.</summary>
        /// <remarks>
        /// A whitelist and not a blacklist. The mine spawns twenty-odd kinds and
        /// the question a challenge floor is asking - will this thing walk towards
        /// me - has to be answered one monster at a time, by reading what each does
        /// at a game tick. Something new added to that list later is excluded until
        /// somebody checks it, which is the right way round for a floor that cannot
        /// end while a monster is alive.
        /// </remarks>
        private static readonly HashSet<Type> Chasers = new()
        {
            typeof(GreenSlime), typeof(BigSlime), typeof(Skeleton), typeof(Bat),
            typeof(Ghost), typeof(ShadowBrute), typeof(ShadowShaman), typeof(Mummy),
            typeof(Serpent), typeof(MetalHead), typeof(RockGolem), typeof(DinoMonster),
            typeof(Leaper), typeof(SquidKid), typeof(Shooter), typeof(BlueSquid),
            typeof(Fly)
        };

        /// <summary>
        /// The monsters no floor wants, wherever it is.
        /// </summary>
        /// <remarks>
        /// The cave bug walks in a straight line until something stops it and then
        /// turns round and walks back, which is the whole of its behaviour; it never
        /// notices the player at all. It can also spawn armoured, and an armoured
        /// bug takes exactly nothing from any weapon without a Bug Killer
        /// enchantment on it - so on any floor that waits for the room to be clear,
        /// one of them is not a dull monster but a dead run.
        /// </remarks>
        private static readonly HashSet<Type> Unwanted = new()
        {
            typeof(Bug)
        };

        /// <summary>Whether <paramref name="monster"/> will come when called.</summary>
        public static bool IsChaser(Monster monster) => Chasers.Contains(monster.GetType());

        /// <summary>Whether this is one the mine should not be spawning at all.</summary>
        public static bool IsUnwanted(Monster monster) => Unwanted.Contains(monster.GetType());

        /// <summary>How many times to ask the mine again before settling.</summary>
        private const int Rerolls = 16;

        /// <summary>
        /// A monster for this depth that will come and fight.
        /// </summary>
        /// <remarks>
        /// Asked for again rather than picked from the list directly, so a floor
        /// keeps the depth's own mix - the mine decides what lives this far down and
        /// this only declines the ones that will not take part. The slime at the end
        /// is for a depth whose whole table is sedentary, which no depth currently
        /// is; better a slime than a monster that never arrives.
        /// </remarks>
        public static Monster ChaserFor(MineShaft mine, int x, int y)
        {
            for (int tries = 0; tries < Rerolls; tries++)
            {
                Monster monster = mine.getMonsterForThisLevel(mine.mineLevel, x, y);

                if (IsChaser(monster))
                    return monster;
            }

            return new GreenSlime(new Vector2(x, y) * 64f, Math.Max(0, mine.mineLevel));
        }
    }
}

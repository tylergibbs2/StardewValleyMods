using StardewValley.Monsters;
using System;
using xTile.Dimensions;

namespace StardewRoguelike.Extensions
{
    internal static class MonsterExtensions
    {
        public static float AdjustRangeForHealth(this Monster monster, float min, float max)
        {
            float range = max - min;
            float hpPercent = (float)monster.Health / monster.MaxHealth;
            float toSubtract = hpPercent * range;
            return Math.Min(Math.Max(min, max - toSubtract), max);
        }

        public static int AdjustRangeForHealth(this Monster monster, int min, int max)
        {
            int range = max - min;
            float hpPercent = (float)monster.Health / monster.MaxHealth;
            int toSubtract = (int)Math.Round(hpPercent * range, 0);
            return Math.Min(Math.Max(min, max - toSubtract), max);
        }

        /// <summary>
        /// The same the other way round: <paramref name="max"/> at full health,
        /// falling to <paramref name="min"/> at none.
        /// </summary>
        /// <remarks>
        /// For the things that should ease off as a boss dies rather than tighten.
        /// <see cref="AdjustRangeForHealth(Monster, int, int)"/> rises as health
        /// falls, which is what a boss's aggression should do - more shots, more
        /// charges, shorter gaps. Applied to the windows where a boss cannot be hit
        /// at all, it does the opposite of what a fight wants: the kill gets slower
        /// and quieter the closer it comes.
        /// </remarks>
        public static int AdjustRangeForHealthInverted(this Monster monster, int min, int max)
        {
            return min + max - monster.AdjustRangeForHealth(min, max);
        }

        public static void KeepInMap(this Monster monster)
        {
            if (monster.Position.X < 0f)
                monster.Position = new(0, monster.Position.Y);
            if (monster.Position.Y < 0f)
                monster.Position = new(monster.Position.X, 0);
            if (monster.Position.X > monster.currentLocation.map.GetLayer("Back").LayerWidth * 64)
                monster.Position = new(monster.currentLocation.map.GetLayer("Back").LayerWidth * 64, monster.Position.Y);
            if (monster.Position.Y > monster.currentLocation.map.GetLayer("Back").LayerHeight * 64)
                monster.Position = new(monster.Position.X, monster.currentLocation.map.GetLayer("Back").LayerHeight * 64);
        }
    }
}

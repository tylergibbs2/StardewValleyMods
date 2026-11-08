using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewRoguelike.VirtualProperties;
using StardewValley;
using StardewValley.Locations;
using System;

namespace StardewRoguelike.Run
{
    /// <summary>
    /// Keeps a floor from outstaying its welcome.
    ///
    /// Mining floors use the base game's ladder roll, which is a chance per stone
    /// broken. That is fine in a farming game where the mines are somewhere you visit;
    /// across the forty-odd floors of a run it is the single longest stretch of a run
    /// where nothing is being decided. The exit still has to be earned, but after a
    /// while it stops being optional.
    /// </summary>
    internal static class RunPacing
    {
        /// <summary>Seconds on a floor before its exit is placed for the player.</summary>
        private const int SecondsBeforeExitIsGiven = 45;

        /// <summary>How far from the entrance the placed exit has to be.</summary>
        private const float MinimumDistanceFromStart = 8f;

        /// <summary>Tiles to try before giving up and leaving it to the base game.</summary>
        private const int PlacementAttempts = 400;

        /// <summary>
        /// Places the exit if the floor has gone long enough without one. Host only;
        /// createLadderDown publishes through a net event, so everyone sees it.
        /// </summary>
        public static void EnsureExit(MineShaft mine)
        {
            if (!Context.IsMainPlayer || mine.ladderHasSpawned)
                return;

            // Floors that decide their own exit: challenges open theirs when they are
            // won, bosses on death, merchants have one from the start.
            if (ChallengeFloor.IsChallengeFloor(mine)
                || Merchant.IsMerchantFloor(mine)
                || BossFloor.IsBossFloor(mine))
                return;

            long entry = mine.get_MineShaftEntryTime().Value;
            if (entry == 0)
                return;

            long now = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();
            if (now - entry < SecondsBeforeExitIsGiven)
                return;

            if (!TryFindExitTile(mine, out Point tile))
                return;

            mine.createLadderDown(tile.X, tile.Y);
        }

        private static bool TryFindExitTile(MineShaft mine, out Point tile)
        {
            tile = Point.Zero;

            int width = mine.Map.Layers[0].LayerWidth;
            int height = mine.Map.Layers[0].LayerHeight;

            for (int attempt = 0; attempt < PlacementAttempts; attempt++)
            {
                int x = Game1.random.Next(width);
                int y = Game1.random.Next(height);

                if (!mine.isTileClearForMineObjects(x, y))
                    continue;

                if (mine.getDistanceFromStart(x, y) <= MinimumDistanceFromStart)
                    continue;

                tile = new Point(x, y);
                return true;
            }

            // A layout with nothing suitable keeps the base game's behaviour rather
            // than dropping an exit somewhere unreachable.
            return false;
        }
    }
}

using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Locations;
using System;
using xTile.Layers;

namespace StardewRoguelike.Patches
{
    [HarmonyPatch(typeof(MineShaft), "findLadder")]
    internal class MineShaftFindLadderAbuse
    {
        /// <summary>The Buildings-layer tile index the game treats as a ladder down.</summary>
        private const int LadderTileIndex = 115;

        /// <summary>
        /// The (tile Y offset, radius) of each light making up the glow around the ladder.
        /// </summary>
        private static readonly (int, float)[] LadderGlowOffsets =
        {
            (-2, 0.25f), (-1, 0.5f), (0, 0.75f), (1, 1f)
        };

        public static bool Prefix(MineShaft __instance)
        {
            int level = Roguelike.GetLevelFromMineshaft(__instance);
            int x = -1;
            int y = -1;
            if (BossFloor.IsBossFloor(level) && BossFloor.GetBossIndexForFloor(level) == 7)
            {
                x = 24;
                y = 10;
            }
            else if (ForgeFloor.IsForgeFloor(__instance))
            {
                x = 8;
                y = 7;
            }
            else if (ChallengeFloor.IsChallengeFloor(__instance))
            {
                Vector2 spawnTile = ChallengeFloor.GetSpawnLocation(__instance);
                if (spawnTile == Vector2.Zero)
                    return true;

                x = (int)spawnTile.X;
                y = (int)spawnTile.Y;
            }

            if (x != -1 && y != -1)
            {
                __instance.tileBeneathLadder = new Vector2(x, y);

                // Light IDs are strings in 1.6, and the dictionary is keyed by them.
                for (int i = 0; i < LadderGlowOffsets.Length; i++)
                {
                    (int yOffset, float radius) = LadderGlowOffsets[i];
                    string lightId = $"{ModEntry.ModId}_LadderGlow_{x}_{y}_{i}";

                    __instance.sharedLights[lightId] = new LightSource(
                        lightId,
                        4,
                        new Vector2(x, y + yOffset) * 64f + new Vector2(32f, 0f),
                        radius,
                        new Color(0, 20, 50),
                        LightSource.LightContext.None,
                        0L
                    );
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// Falls back to finding the ladder the way 1.5.6 did.
        ///
        /// 1.6's findLadder only considers tiles drawn from the tilesheet named
        /// "mine" (<c>GetTileIndexAt(x, y, "mine")</c>); 1.5.6 read the tile index
        /// off the Buildings layer regardless of tilesheet. Every boss arena draws
        /// its ladder from its own themed sheet, so none of them registered any more
        /// and tileBeneathLadder stayed at Vector2.Zero — which is where the player
        /// got warped, outside the map.
        /// </summary>
        public static void Postfix(MineShaft __instance)
        {
            if (__instance.tileBeneathLadder != Vector2.Zero)
                return;

            Layer? buildings = __instance.map?.GetLayer("Buildings");
            if (buildings is null)
                return;

            for (int y = 0; y < buildings.LayerHeight; y++)
            {
                for (int x = 0; x < buildings.LayerWidth; x++)
                {
                    if (buildings.Tiles[x, y]?.TileIndex == LadderTileIndex)
                    {
                        __instance.tileBeneathLadder = new Vector2(x, y + 1);
                        return;
                    }
                }
            }
        }
    }
}

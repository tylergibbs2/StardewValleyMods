using HarmonyLib;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using StardewRoguelike.VirtualProperties;
using StardewValley;
using StardewValley.Locations;
using xTile.Tiles;
using System;

namespace StardewRoguelike.Patches
{
    [HarmonyPatch(typeof(MineShaft), nameof(MineShaft.loadLevel))]
    internal class LoadLevelPatch
    {
        /// <summary>The tilesheet ID 1.6 expects a mine level to carry.</summary>
        private const string MineTileSheetId = "mine";

        /// <summary>
        /// Gives the map's mine-derived tilesheet the ID vanilla looks for, so the
        /// game's own mine-tile lookups resolve instead of throwing. Only sheets
        /// already named after the mine art are renamed, so sheets the mod addresses
        /// by name itself (bugLandTiles, z_Town) are left alone.
        /// </summary>
        private static void AliasMineTileSheet(MineShaft mine)
        {
            if (mine.map is null)
                return;

            foreach (TileSheet sheet in mine.map.TileSheets)
            {
                if (sheet.Id.Contains(MineTileSheetId, StringComparison.OrdinalIgnoreCase))
                {
                    sheet.Id = MineTileSheetId;
                    return;
                }
            }
        }

        public static bool Prefix(MineShaft __instance, int level)
        {
            bool loadedDarkArea = false;

            bool isMonsterArea = false;
            bool isSlimeArea = false;
            bool isQuarryArea = false;
            bool isDinoArea = false;

            string mapToLoad = Roguelike.GetMapPath(__instance, avoidWater: __instance.get_MineShaftIsDarkArea());
            __instance.mapPath.Value = "Maps\\Mines\\" + mapToLoad;
            __instance.get_MineShaftLoadedMap().Value = mapToLoad;
            __instance.updateMap();

            // 1.6 expects every mine level to have a tilesheet with the ID "mine", and
            // throws KeyNotFoundException when it doesn't. Four separate places rely on
            // it: the mapImageSource change handler, ApplyDiggableTileFixes,
            // doCreateLadderDown (when the ladder spawns) and setElevatorLit (during
            // resetLocalState, where the throw leaves the screen faded to black).
            //
            // The mod's themed arenas name their sheet 1_mine, 1_mine_dino, mine_lava
            // and so on, so give whichever one that is the ID vanilla looks for. The
            // retint is still skipped for them, because pointing it at Maps/Mines/mine
            // would replace the arena's own art.
            bool canRetintMineTiles = __instance.map?.GetTileSheet("mine") is not null;
            if (!canRetintMineTiles)
                AliasMineTileSheet(__instance);

            if (canRetintMineTiles)
            {
                if (__instance.getMineArea(level) < 40 && !BossFloor.IsBossFloor(__instance))
                    __instance.mapImageSource.Value = "Maps\\Mines\\mine";
                else if (__instance.getMineArea(level) == 40)
                    __instance.mapImageSource.Value = "Maps\\Mines\\mine_frost";
                else if (__instance.getMineArea(level) == 80)
                    __instance.mapImageSource.Value = "Maps\\Mines\\mine_lava";
                else if (__instance.getMineArea(level) == 121)
                    __instance.mapImageSource.Value = "Maps\\Mines\\mine_desert";
            }

            if (__instance.get_MineShaftIsDarkArea())
            {
                if (canRetintMineTiles)
                    __instance.mapImageSource.Value += "_dark";
                loadedDarkArea = true;
            }

            if (canRetintMineTiles && __instance.GetAdditionalDifficulty() > 0 && !BossFloor.IsBossFloor(__instance))
            {
                string map_image_source = "Maps\\Mines\\mine";
                if (__instance.mapImageSource.Value is not null)
                    map_image_source = __instance.mapImageSource.Value;
                if (map_image_source.EndsWith("_dark"))
                {
                    map_image_source = map_image_source.Remove(map_image_source.Length - "_dark".Length);
                    loadedDarkArea = true;
                }

                string base_map_image_source = map_image_source;
                if (loadedDarkArea)
                    map_image_source += "_dark";
                map_image_source += "_dangerous";

                try
                {
                    __instance.mapImageSource.Value = map_image_source;
                    Game1.temporaryContent.Load<Texture2D>(__instance.mapImageSource.Value);
                }
                catch (ContentLoadException)
                {
                    map_image_source = base_map_image_source + "_dangerous";
                    try
                    {
                        __instance.mapImageSource.Value = map_image_source;
                        Game1.temporaryContent.Load<Texture2D>(__instance.mapImageSource.Value);
                    }
                    catch (ContentLoadException)
                    {
                        map_image_source = base_map_image_source;
                        if (loadedDarkArea)
                            map_image_source += "_dark";

                        try
                        {
                            __instance.mapImageSource.Value = map_image_source;
                            Game1.temporaryContent.Load<Texture2D>(__instance.mapImageSource.Value);
                        }
                        catch (ContentLoadException)
                        {
                            __instance.mapImageSource.Value = base_map_image_source;
                        }
                    }
                }
            }

            if (canRetintMineTiles)
                __instance.ApplyDiggableTileFixes();
            if (!__instance.isSideBranch())
            {
                MineShaft.lowestLevelReached = Math.Max(MineShaft.lowestLevelReached, level);
                __instance.setMapTile(0, 0, 0, "Buildings", __instance.map.TileSheets[0].Id);
            }

            if (loadedDarkArea)
                __instance.set_MineShaftIsDarkArea(loadedDarkArea);

            __instance.isMonsterArea = isMonsterArea;
            __instance.isSlimeArea = isSlimeArea;
            __instance.isQuarryArea = isQuarryArea;
            __instance.isDinoArea = isDinoArea;

            __instance.loadedDarkArea = loadedDarkArea;

            return false;
        }
    }
}

using HarmonyLib;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using StardewRoguelike.VirtualProperties;
using StardewValley;
using StardewValley.Locations;
using System;

namespace StardewRoguelike.Patches
{
    [HarmonyPatch(typeof(MineShaft), nameof(MineShaft.loadLevel))]
    internal class LoadLevelPatch
    {
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

            // 1.6 validates that the map has a tilesheet with the ID "mine" whenever
            // mapImageSource changes, and throws KeyNotFoundException if it doesn't.
            // A few of the mod's arenas ship their own tilesheet (bugLandTiles,
            // mine_lava, 1_mine), so there is nothing for the mine retint to apply to
            // and the level load used to blow up on them.
            bool canRetintMineTiles = __instance.map?.GetTileSheet("mine") is not null;

            if (canRetintMineTiles && __instance.getMineArea(level) < 40 && !BossFloor.IsBossFloor(__instance))
                __instance.mapImageSource.Value = "Maps\\Mines\\mine";
            else if (__instance.getMineArea(level) == 40)
                __instance.mapImageSource.Value = "Maps\\Mines\\mine_frost";
            else if (__instance.getMineArea(level) == 80)
                __instance.mapImageSource.Value = "Maps\\Mines\\mine_lava";
            else if (__instance.getMineArea(level) == 121)
                __instance.mapImageSource.Value = "Maps\\Mines\\mine_desert";

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

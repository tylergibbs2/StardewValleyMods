using HarmonyLib;
using StardewRoguelike.Extensions;
using StardewValley;
using StardewValley.Locations;

namespace StardewRoguelike.Patches
{
    [HarmonyPatch(typeof(DwarfGate), "ApplyTiles")]
    internal class DwarfGateApplyTiles
    {
        private const string DungeonTileSheetId = MineShaftExtensions.DungeonTileSheetId;

        private static int GetTileIndex(xTile.Map map, int x, int y)
        {
            return Roguelike.GetTileIndexForMap(map, DungeonTileSheetId, x + y * 16, doSum: true);
        }

        public static void Postfix(DwarfGate __instance)
        {
            // Use the location's live map, not a fresh copy from content: the live one
            // is where CreateDwarfGate aliased the dungeon tilesheet, and the tile is
            // written back to it below.
            xTile.Map map = __instance.locationRef.Value.map;
            if (map is null)
                return;

            if (__instance.localOpened)
                __instance.locationRef.Value.removeTile(__instance.tilePosition.X, __instance.tilePosition.Y + 1, "Buildings");
            else
            {
                int tileIndex = GetTileIndex(map, 0, 0);
                __instance.locationRef.Value.setMapTile(__instance.tilePosition.X, __instance.tilePosition.Y + 1, tileIndex, "Buildings", DungeonTileSheetId);
            }
        }
    }
}

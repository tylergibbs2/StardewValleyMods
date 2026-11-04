using Microsoft.Xna.Framework;
using StardewRoguelike.VirtualProperties;
using StardewValley;
using xTile.Tiles;
using StardewValley.Locations;
using StardewValley.Monsters;
using System.Collections.Generic;

namespace StardewRoguelike.Extensions
{
    public static class MineShaftExtensions
    {
        public static void SpawnMonsters(this MineShaft mine, int amount)
        {
            float spawnChance = 0.022f;

            // Each pass sweeps the map and places a monster on a small chance per
            // tile, so a map offering no tile that is both clear and far enough from
            // the start would sweep forever. Give up instead of hanging.
            int sweeps = 0;
            while (amount > 0 && sweeps++ < 200)
            {
                for (int i = 0; i < mine.map.GetLayer("Back").LayerWidth; i++)
                {
                    if (amount == 0)
                        break;
                    for (int j = 0; j < mine.map.GetLayer("Back").LayerHeight; j++)
                    {
                        if (amount == 0)
                            break;
                        else if (!mine.isTileClearForMineObjects(i, j))
                            continue;

                        if (Game1.random.NextDouble() <= spawnChance && mine.getDistanceFromStart(i, j) > 5f)
                        {
                            Monster monster = mine.BuffMonsterIfNecessary(mine.getMonsterForThisLevel(mine.mineLevel, i, j));
                            Roguelike.AdjustMonster(mine, ref monster);
                            mine.characters.Add(monster);
                            amount--;
                        }
                    }
                }
            }
        }

        public static void SetTile(this MineShaft mine, int x, int y, string layer, string tileSheetId, int tileId, int animationInterval = -1, int[] animationOrder = null)
        {
            int tileIndex = Roguelike.GetTileIndexForMap(mine.map, tileSheetId, tileId);
            if (animationInterval != -1 && animationOrder is not null)
                mine.setAnimatedMapTile(x, y, animationOrder, animationInterval, layer, tileSheetId);
            else
                mine.setMapTile(x, y, tileIndex, layer, tileSheetId);
        }

        /// <summary>The tilesheet ID 1.6's dwarf-gate code expects.</summary>
        public const string DungeonTileSheetId = "dungeon";

        /// <summary>
        /// Makes sure the map has a tilesheet named <see cref="DungeonTileSheetId"/>,
        /// renaming the mod's "volcano_dungeon" sheet if that is what it has. Maps
        /// that already carry both are left alone.
        /// </summary>
        private static void EnsureDungeonTileSheetId(MineShaft mine)
        {
            if (mine.map is null || mine.map.GetTileSheet(DungeonTileSheetId) is not null)
                return;

            if (mine.map.GetTileSheet("volcano_dungeon") is TileSheet volcanoSheet)
                volcanoSheet.Id = DungeonTileSheetId;
        }

        public static DwarfGate CreateDwarfGate(this MineShaft mine, int index, Point gatePosition, Point switchPosition)
        {
            // DwarfGate's constructor ends by calling ApplyTiles, which writes tiles
            // through locationRef - still the throwaway dungeon at that point. Its map
            // is never loaded, so give it the mine's; the tiles then land where the
            // caller is about to point the gate anyway.
            // 1.6's DwarfGate.ApplyTiles addresses its tilesheet by the ID "dungeon".
            // 1.5.6 addressed tilesheets by index, so the mod's maps were free to call
            // theirs "volcano_dungeon"; alias it so the vanilla gate code can find it.
            EnsureDungeonTileSheetId(mine);

            VolcanoDungeon fake = new(index);
            fake.possibleSwitchPositions[1] = new() { switchPosition };
            fake.mapPath.Value = mine.mapPath.Value;
            fake.map = mine.map;

            DwarfGate gate = new(fake, index, gatePosition.X, gatePosition.Y, 0);
            gate.locationRef.Value = mine;

            // fixes memory leak, do not remove
            fake = null;

            gate.ApplyTiles();
            mine.get_MineShaftDwarfGates().Add(gate);

            return gate;
        }

        public static void SpawnLocalChest(this MineShaft mine, Vector2 tileLocation)
        {
            var chests = mine.get_MineShaftNetChests();
            chests.Add(new(tileLocation));
        }

        public static void SpawnLocalChest(this MineShaft mine, Vector2 tileLocation, long mustOpenBy)
        {
            var chests = mine.get_MineShaftNetChests();
            chests.Add(new(tileLocation, mustOpenBy));
        }

        public static void SpawnLocalChest(this MineShaft mine, Vector2 tileLocation, Item item)
        {
            List<Item> items = new()
            {
                item
            };
            SpawnLocalChest(mine, tileLocation, items);
        }

        public static void SpawnLocalChest(this MineShaft mine, Vector2 tileLocation, Item item, long mustOpenBy)
        {
            List<Item> items = new()
            {
                item
            };
            SpawnLocalChest(mine, tileLocation, items, mustOpenBy);
        }

        public static void SpawnLocalChest(this MineShaft mine, Vector2 tileLocation, List<Item> items)
        {
            var chests = mine.get_MineShaftNetChests();
            chests.Add(new(tileLocation, items));
        }

        public static void SpawnLocalChest(this MineShaft mine, Vector2 tileLocation, List<Item> items, long mustOpenBy)
        {
            var chests = mine.get_MineShaftNetChests();
            chests.Add(new(tileLocation, items, mustOpenBy));
        }

        public static bool TryToAddMonster(this MineShaft mine, Monster m, int tileX, int tileY)
        {
            if (mine.isTileClearForMineObjects(tileX, tileY) && !mine.IsTileBlockedBy(new Vector2(tileX, tileY)))
            {
                m.setTilePosition(tileX, tileY);
                mine.characters.Add(m);
                return true;
            }

            return false;
        }

        public static bool IsAreaClearForObjects(this MineShaft mine, Vector2 topLeftTile, int width, int height)
        {
            int topLeftX = (int)topLeftTile.X;
            int topLeftY = (int)topLeftTile.Y;

            for (int i = topLeftX; i < topLeftX + width; i++)
            {
                for (int j = topLeftY; j < topLeftY + height; j++)
                {
                    if (!mine.isTileClearForMineObjects(i, j))
                        return false;
                }
            }

            return true;
        }

        public static bool IsNormalFloor(this MineShaft mine)
        {
            return !ChallengeFloor.IsChallengeFloor(mine) && !ChestFloor.IsChestFloor(mine) && !Merchant.IsMerchantFloor(mine) && !BossFloor.IsBossFloor(mine) && !ForgeFloor.IsForgeFloor(mine);
        }
    }
}
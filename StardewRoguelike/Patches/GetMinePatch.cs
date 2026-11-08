using StardewValley.Locations;
using System;
using System.Reflection;
using StardewRoguelike.VirtualProperties;
using StardewValley;
using StardewRoguelike.Extensions;
using StardewRoguelike.Run;
using HarmonyLib;

namespace StardewRoguelike.Patches
{
    [HarmonyPatch(typeof(MineShaft), nameof(MineShaft.GetMine))]
    internal class GetMinePatch
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("SMAPI.CommonErrors", "AvoidNetField:Avoid Netcode types when possible", Justification = "The property is read-only.")]
        public static bool Prefix(ref MineShaft __result, string name)
        {
            // requestedFloor/requestedLevel
            string format = name["UndergroundMine".Length..];
            string floorString = format.Split("/")[0];
            string levelString = format.Split("/")[1];

            int requestedFloor = Convert.ToInt32(floorString);
            int requestedLevel = Convert.ToInt32(levelString);

            foreach (MineShaft mine in MineShaft.activeMines)
            {
                if (MineShaftLevel.get_MineShaftLevel(mine).Value == requestedLevel)
                {
                    __result = mine;
                    return false;
                }
            }

            MineShaft newMine = new(0);
            newMine.mineRandom = Roguelike.FloorRng;
            newMine.get_MineShaftLevel().Value = requestedLevel;
            newMine.get_MineShaftEntryTime().Value = ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds();

            // What this floor is comes from the run plan and the group's choices now,
            // rather than from a chain of predicates each rolling FloorRng and reading
            // whichever mines happened to be loaded. Two machines generating the same
            // level therefore build the same floor.
            FloorKind kind = RunRoute.KindFor(requestedLevel);
            switch (kind)
            {
                case FloorKind.Merchant:
                    Merchant.Initialize(newMine);
                    break;

                case FloorKind.Boss:
                    BossFloor.Setup(newMine);
                    break;

                case FloorKind.Forge:
                    newMine.get_MineShaftForgeFloor().Value = true;
                    break;

                case FloorKind.Vault:
                    // Treasure is a family, the way a fight is. Most of it is the
                    // plain chest floor; the rest are the loot floors that had no
                    // way of being reached once the plan stopped asking for them.
                    ChallengeFloor.ChallengeType? treasure = RunRoute.VaultFor(requestedLevel);

                    if (treasure is null)
                    {
                        newMine.get_MineShaftChestFloor().Value = true;
                        ChestFloor.SpawnChest(newMine);
                    }
                    else
                    {
                        newMine.get_MineShaftIsChallengeFloor().Value = true;
                        newMine.get_MineShaftChallengeFloor().Value = ChallengeFloor.Create(treasure.Value);
                    }

                    break;

                case FloorKind.Arena:
                    newMine.get_MineShaftIsChallengeFloor().Value = true;
                    newMine.get_MineShaftChallengeFloor().Value =
                        ChallengeFloor.Create(RunRoute.ArenaFor(requestedLevel));
                    break;

                case FloorKind.Cauldron:
                    // Placed further down, for the same reason as the shrine: the
                    // map does not exist yet here.
                    break;

                case FloorKind.Shrine:
                    // Placed further down. The map does not exist yet here, and the
                    // shrine has to pick a tile to stand on.
                    break;

                case FloorKind.Spring:
                    newMine.get_MineShaftIsChallengeFloor().Value = true;
                    newMine.get_MineShaftChallengeFloor().Value =
                        ChallengeFloor.Create(ChallengeFloor.ChallengeType.HotSpring);
                    break;

                case FloorKind.DarkMineshaft:
                    // Set here rather than below with the hard mode roll, because the
                    // map is chosen from it: a dark floor avoids the water maps.
                    newMine.set_MineShaftIsDarkArea(true);
                    break;

                case FloorKind.Normal:
                default:
                    break;
            }

            double darkChance;
            if (DebugCommands.ForcedDarkChance > 0f)
                darkChance = DebugCommands.ForcedDarkChance;
            else
                darkChance = 0.2;

            if (Roguelike.HardMode && Roguelike.FloorRng.NextDouble() < darkChance && newMine.IsNormalFloor())
                newMine.set_MineShaftIsDarkArea(true);

            int depth = Roguelike.GetFloorDepth(newMine, requestedFloor);

            newMine.mineLevel = depth;
            newMine.name.Value = name;

            MineShaft.activeMines.Add(newMine);

            newMine.GetType().GetMethod("generateContents", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(newMine, null);

			__result = newMine;

            Game1.netWorldState.Value.MinesDifficulty = 0;
            Game1.netWorldState.Value.SkullCavesDifficulty = 0;

            // Anything that reads the map waits until generateContents has built it.
            // Challenge floors already did; the shrine has to as well, since it looks
            // for a clear tile near the entrance and neither the tiles nor the
            // entrance exist before this point.
            if (kind == FloorKind.Shrine)
                TerrainFeatures.PerkShrine.PlaceIn(newMine);

            if (kind == FloorKind.Cauldron)
                TerrainFeatures.CleansingCauldron.PlaceIn(newMine);

            if (newMine.get_MineShaftIsChallengeFloor().Value)
                newMine.get_MineShaftChallengeFloor().Value.Initialize(newMine);

            return false;
        }
    }
}

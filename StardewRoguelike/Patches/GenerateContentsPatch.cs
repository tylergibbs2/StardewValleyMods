using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewRoguelike.Extensions;
using StardewValley.Locations;
using System.Reflection;

namespace StardewRoguelike.Patches
{
    [HarmonyPatch(typeof(MineShaft), "generateContents")]
    internal class GenerateContentsPatch
    {
        public static bool Prefix(MineShaft __instance)
        {
            __instance.ladderHasSpawned = false;
            __instance.loadedDarkArea = false;
            __instance.loadLevel(__instance.mineLevel);

            __instance.isMonsterArea = false;
            __instance.isSlimeArea = false;
            __instance.isQuarryArea = false;
            __instance.isDinoArea = false;
            __instance.lighting = new Color(80, 80, 40);

            MineShaft.permanentMineChanges.Clear();

            __instance.findLadder();
            __instance.GetType().GetMethod("populateLevel", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(__instance, null);

            if (__instance.IsNormalFloor() || BossFloor.IsBossFloor(__instance))
                __instance.isMonsterArea = true;

            return false;
        }
    }
}

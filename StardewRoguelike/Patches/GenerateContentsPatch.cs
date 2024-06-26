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
            __instance.GetType().GetField("ladderHasSpawned", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(__instance, false);
            __instance.GetType().GetField("loadedDarkArea", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(__instance, false);
            __instance.loadLevel(__instance.mineLevel);

            __instance.GetType().GetProperty("isMonsterArea", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(__instance, false);
            __instance.GetType().GetProperty("isSlimeArea", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(__instance, false);
            __instance.GetType().GetProperty("isQuarryArea", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(__instance, false);
            __instance.GetType().GetProperty("isDinoArea", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(__instance, false);
            __instance.GetType().GetProperty("lighting", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(__instance, new Color(80, 80, 40));

            MineShaft.permanentMineChanges.Clear();

            __instance.findLadder();
            __instance.GetType().GetMethod("populateLevel", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(__instance, null);

            if (__instance.IsNormalFloor() || BossFloor.IsBossFloor(__instance))
                __instance.GetType().GetProperty("isMonsterArea", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(__instance, true);

            return false;
        }
    }
}

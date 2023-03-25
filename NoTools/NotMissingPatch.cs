using System.Collections.Generic;
using System;
using HarmonyLib;
using StardewValley;

namespace NoTools
{
    [HarmonyPatch(typeof(Game1), "checkIsMissingTool")]
    internal class NotMissingPatch
    {
        public static bool Prefix(Dictionary<Type, int> missingTools, ref int missingScythes, Item item)
        {
            missingTools.Clear();
            missingScythes = 0;
            return false;
        }
    }
}

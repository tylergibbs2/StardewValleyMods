using HarmonyLib;
using StardewValley;
using StardewValley.Menus;
using System;
using System.IO;
using System.Reflection;

namespace StardewRoguelike.Patches
{
    // 1.6 replaced LoadGameMenu() with LoadGameMenu(string filter = null). Harmony
    // resolves a constructor patch with no argument types to the parameterless
    // constructor, which no longer exists, so the types have to be named.
    [HarmonyPatch(typeof(LoadGameMenu), MethodType.Constructor, new Type[] { typeof(string) })]
    internal class LoadGameMenuPatch
    {
        public static bool Prefix(LoadGameMenu __instance)
        {
            if (__instance is CoopMenu || __instance is FarmhandMenu)
                return true;

            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, $@"assets/Saves/{Constants.SaveFile}/SaveGameInfo");
            SaveGame.Load(path);
            Game1.exitActiveMenu();

            return false;
        }
    }
}

using HarmonyLib;
using StardewValley;
using StardewValley.Menus;
using System.Collections.Generic;

namespace StardewRoguelike.Patches
{
    [HarmonyPatch(typeof(LoadGameMenu), "FindSaveGames")]
    class DisableOtherSaves
    {
        public static bool Prefix(ref List<Farmer> __result)
        {
            __result = new List<Farmer>();

            Farmer? farmer = BundledSave.ReadFarmer();
            if (farmer is not null)
                __result.Add(farmer);

            return false;
        }
    }

    [HarmonyPatch(typeof(SaveGame), nameof(SaveGame.Load))]
    class DisableOtherSaves2
    {
        public static bool Prefix()
        {
            Game1.gameMode = 6;
            Game1.loadingMessage = Game1.content.LoadString("Strings\\StringsFromCSFiles:SaveGame.cs.4690");
            Game1.currentLoader = SaveGame.getLoadEnumerator(BundledSave.DataPath);

            return false;
        }
    }
}

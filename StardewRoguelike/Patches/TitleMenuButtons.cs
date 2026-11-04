using HarmonyLib;
using StardewRoguelike.Mode;
using StardewValley.Menus;
using System.Collections.Generic;
using System.Reflection;
using static StardewValley.Menus.LoadGameMenu;

namespace StardewRoguelike.Patches
{
    [AlwaysActive]
    [HarmonyPatch(typeof(CoopMenu), "connectionFinished")]
    class CoopMenuNoNewFarm
    {
        public static void Postfix(CoopMenu __instance)
        {
            // Only the roguelike forbids starting a brand new farm from co-op.
            if (!RoguelikeMode.IsActive)
                return;

            List<MenuSlot> slots = (List<MenuSlot>)__instance.GetType().GetField("hostSlots", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(__instance)!;
            slots.RemoveAll(s => s.GetType().Name == "HostNewFarmSlot");
        }
    }
}

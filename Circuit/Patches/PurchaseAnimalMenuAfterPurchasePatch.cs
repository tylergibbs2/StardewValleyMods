using System.Reflection;
using HarmonyLib;
using StardewValley;
using StardewValley.Menus;

namespace Circuit.Patches
{
    [HarmonyPatch(typeof(PurchaseAnimalsMenu), nameof(PurchaseAnimalsMenu.marnieAnimalPurchaseMessage))]
    internal class PurchaseAnimalMenuAfterPurchasePatch
    {
        public static void Postfix(PurchaseAnimalsMenu __instance)
        {
            if (!ModEntry.ShouldPatch())
                return;

            FarmAnimal animal = (FarmAnimal)__instance.GetType().GetField("animalBeingPurchased", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(__instance)!;
            ModEntry.Instance.TaskManager?.OnAnimalPurchased(animal);
        }
    }
}

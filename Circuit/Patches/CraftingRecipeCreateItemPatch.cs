using System.Reflection;
using HarmonyLib;
using StardewValley;
using StardewValley.Menus;

namespace Circuit.Patches
{
    [HarmonyPatch(typeof(CraftingPage), "clickCraftingRecipe")]
    internal class CraftingRecipeCreateItemPatch
    {
        public static void Postfix(CraftingPage __instance, ClickableTextureComponent c)
        {
            if (!ModEntry.ShouldPatch())
                return;

            int currentCraftingPage = (int)__instance.GetType().GetField("currentCraftingPage", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(__instance)!;
            Item crafted = __instance.pagesOfCraftingRecipes[currentCraftingPage][c].createItem();

            ModEntry.Instance.TaskManager?.OnItemCrafted(crafted);
        }
    }
}

using System;
using System.Collections.Generic;
using HarmonyLib;
using StardewValley;

namespace Circuit.Patches
{
    [HarmonyPatch(typeof(Farmer), nameof(Farmer.addItemToInventory), new Type[] { typeof(Item), typeof(List<Item>) })]
    internal class FarmerAddItemToInventoryPatch
    {
        public static bool Prefix(Item item)
        {
            if (!ModEntry.ShouldPatch() || item is null)
                return true;

            ModEntry.Instance.TaskManager?.OnItemObtained(item);
            ModEntry.Instance.EventManager?.OnItemObtained(item);

            return true;
        }
    }
}

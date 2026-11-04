using HarmonyLib;
using StardewValley;

namespace StardewRoguelike.Patches
{
    [HarmonyPatch(typeof(Farmer), "isWearingRing")]
    internal class FarmerIsWearingRingPatch
    {
        public static void Postfix(ref bool __result, string itemId)
        {
            if (itemId == "525" && Perks.HasPerk(Perks.PerkType.Sturdy))
                __result = true;
        }
    }
}

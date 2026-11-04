using HarmonyLib;
using StardewRoguelike.VirtualProperties;
using StardewValley;
using StardewValley.Tools;
using System;

namespace StardewRoguelike.Patches
{
    [HarmonyPatch(typeof(FishingRod), "doDoneFishing")]
    internal class FishingRodDonePatch
    {
        public static void Postfix(FishingRod __instance)
        {
            Farmer lastUser = __instance.lastUser;
            if (lastUser is null || !lastUser.IsLocalPlayer)
                return;

            __instance.increment_FishingRodUses();

            int maxUses = Perks.HasPerk(Perks.PerkType.Fisherman) ? 10 : 5;
            if (__instance.get_FishingRodUses() >= maxUses)
            {
                Game1.player.removeItemFromInventory(__instance);
                Game1.playSound("breakingGlass");
            }
        }
    }
}

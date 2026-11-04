using HarmonyLib;
using StardewValley;
using StardewValley.Locations;
using System;
using xTile.Dimensions;

namespace StardewRoguelike.Patches
{
    // 1.6 split performAction into a raw-string overload that defers to this one, so
    // the args arrive already tokenized.
    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.performAction), new Type[] { typeof(string[]), typeof(Farmer), typeof(Location) })]
    internal class PerformActionPatch
    {
        public static bool Prefix(GameLocation __instance, ref bool __result, string[] action, Farmer who, Location tileLocation)
        {
            if (action is not null && action.Length > 0 && who.IsLocalPlayer)
            {
                bool result = false;
                string[] actionParams = action;
                switch (actionParams[0])
                {
                    case "Buy":
                        // openShopMenu is gone in 1.6; the roguelike's own shop names
                        // are dispatched by the mod instead.
                        __result = actionParams.Length > 1 && Merchant.TryOpenShop(__instance, actionParams[1]);
                        return false;
                }

                if (__instance is MineShaft mine)
                {
                    result = Merchant.PerformAction(mine, actionParams[0], Game1.player, tileLocation);
                    if (!result && ChallengeFloor.IsChallengeFloor(mine))
                        result = ChallengeFloor.PerformAction(mine, string.Join(' ', action), Game1.player, tileLocation);

                    if (result)
                    {
                        __result = true;
                        return false;
                    }
                }
                else if (__instance is Mine lobby)
                {
                    result = Roguelike.PerformAction(lobby, actionParams[0], Game1.player, tileLocation);
                }

                if (result)
                {
                    __result = true;
                    return false;
                }
            }

            return true;
        }
    }
}

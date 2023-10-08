using Microsoft.Xna.Framework;
using StardewValley.Locations;
using StardewRoguelike.VirtualProperties;
using HarmonyLib;

namespace StardewRoguelike.Patches
{
    [HarmonyPatch(typeof(MineShaft), nameof(MineShaft.mineEntrancePosition))]
    internal class MineShaftEntrancePosition
    {
        public static bool Prefix(MineShaft __instance, ref Vector2 __result)
        {
            Vector2 customDest = __instance.get_MineShaftCustomDestination().Value;
            if (customDest != Vector2.Zero)
            {
                __result = customDest;
                return false;
            }

            return true;
        }
    }
}

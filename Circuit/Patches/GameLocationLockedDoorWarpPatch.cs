using System;
using HarmonyLib;
using StardewValley;

namespace Circuit.Patches
{
    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.lockedDoorWarp))]
    internal class GameLocationLockedDoorWarpPatch
    {
        public static bool Prefix(ref string[] actionParams)
        {
            if (!ModEntry.Instance.IsActiveForSave || !EventManager.EventIsActive(EventType.PoorService))
                return true;

            if (actionParams.Length >= 5)
            {
                int originalOpen = Convert.ToInt32(actionParams[4]);
                actionParams[4] = (originalOpen + 200).ToString();
            }

            return true;
        }
    }
}

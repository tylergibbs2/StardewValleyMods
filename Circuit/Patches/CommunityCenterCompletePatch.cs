using HarmonyLib;
using StardewValley.Locations;

namespace Circuit.Patches
{
    [HarmonyPatch(typeof(CommunityCenter), "doAreaCompleteReward")]
    internal class CommunityCenterCompletePatch
    {
        public static void Postfix(int whichArea)
        {
            if (!ModEntry.ShouldPatch())
                return;

            ModEntry.Instance.TaskManager?.OnCommunityCenterRoomComplete(whichArea);
        }
    }
}

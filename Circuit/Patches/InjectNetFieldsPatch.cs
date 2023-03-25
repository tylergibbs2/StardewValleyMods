using Circuit.VirtualProperties;
using HarmonyLib;
using StardewValley;

namespace Circuit.Patches
{
    [HarmonyPatch(typeof(FarmerTeam), MethodType.Constructor)]
    internal class InjectFarmerTeamNetFields
    {
        public static void Postfix(FarmerTeam __instance)
        {
            __instance.NetFields.AddFields(
                __instance.get_FarmerTeamCurrentEvent()
            );
        }
    }
}

using HarmonyLib;
using StardewRoguelike.VirtualProperties;
using StardewValley;
using StardewValley.Locations;

namespace StardewRoguelike.Patches
{
    [HarmonyPatch(typeof(MineShaft), "initNetFields")]
    internal class InjectMineShaftNetFieldsPatch
    {
        public static void Postfix(MineShaft __instance)
        {
            __instance.NetFields
                .AddField(__instance.get_MineShaftLevel())
                .AddField(__instance.get_MineShaftChestFloor())
                .AddField(__instance.get_MineShaftForgeFloor())
                .AddField(__instance.get_MineShaftCustomDestination())
                .AddField(__instance.get_MineShaftEntryTime())
                .AddField(__instance.get_MineShaftChallengeFloor())
                .AddField(__instance.get_MineShaftIsChallengeFloor())
                .AddField(__instance.get_MineShaftDwarfGates())
                .AddField(__instance.get_MineShaftNetChests())
                .AddField(__instance.get_MineShaftLoadedMap());
        }
    }

    [HarmonyPatch(typeof(Farmer), "farmerInit")]
    internal class InjectFarmerNetFieldsPatch
    {
        public static void Postfix(Farmer __instance)
        {
            __instance.NetFields
                .AddField(__instance.get_FarmerIsSpectating())
                .AddField(__instance.get_FarmerWasDamagedOnThisLevel())
                .AddField(__instance.get_FarmerCurrentLevel())
                .AddField(__instance.get_FarmerCurses());
        }
    }

    [HarmonyPatch(typeof(DwarfGate), "InitNetFields")]
    internal class InjectDwarfGateNetFieldsPatch
    {
        public static void Postfix(DwarfGate __instance)
        {
            __instance.NetFields.AddField(__instance.get_DwarfGateDisabled());
        }
    }

    [HarmonyPatch(typeof(GameLocation), "initNetFields")]
    internal class InjectGameLocationNetFields
    {
        public static void Postfix(GameLocation __instance)
        {
            __instance.NetFields.AddField(__instance.get_DebuffPlayerEvent());
        }
    }

    [HarmonyPatch(typeof(FarmerTeam), MethodType.Constructor)]
    internal class InjectFarmerTeamNetFields
    {
        public static void Postfix(FarmerTeam __instance)
        {
            __instance.NetFields
                .AddField(__instance.get_FarmerTeamHardMode())
                .AddField(__instance.get_FarmerTeamFloorRngSeed());
        }
    }
}

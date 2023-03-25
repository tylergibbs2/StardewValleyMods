using HarmonyLib;
using StardewValley;

namespace Circuit.Patches
{
    [HarmonyPatch(typeof(NPC), nameof(NPC.checkAction))]
    internal class NPCCheckActionPatch
    {
        public static void Postfix(NPC __instance, bool __result, Farmer who)
        {
            if (!ModEntry.ShouldPatch() || !__result)
                return;

            ModEntry.Instance.TaskManager?.OnNPCCheckAction(__instance, who);
        }
    }
}

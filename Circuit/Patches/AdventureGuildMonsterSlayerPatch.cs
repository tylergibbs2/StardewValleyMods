using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using StardewValley.Locations;

namespace Circuit.Patches
{
    [HarmonyPatch(typeof(AdventureGuild), nameof(AdventureGuild.willThisKillCompleteAMonsterSlayerQuest))]
    internal class AdventureGuildMonsterSlayerPatch
    {
        public static void Postfix(bool __result)
        {
            if (!ModEntry.ShouldPatch() || !__result)
                return;

            ModEntry.Instance.TaskManager?.OnMonsterSlayerQuestCompleted();
        }
    }
}

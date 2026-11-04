using HarmonyLib;
using StardewValley;
using StardewValley.Buffs;

namespace StardewRoguelike.Patches
{
    /// <summary>
    /// Caps the nausea debuff at 10 seconds; a full-length one is punishing in a run.
    ///
    /// 1.5.6 could postfix the Buff(int which) constructor. 1.6 removed it and gives
    /// every buff a string ID applied through the buff manager, so the cap is applied
    /// where the buff is handed over instead.
    /// </summary>
    [HarmonyPatch(typeof(BuffManager), nameof(BuffManager.Apply))]
    internal class ShorterNauseousPatch
    {
        private const int MaxNauseaDuration = 10 * 1000;

        public static void Prefix(Buff buff)
        {
            if (buff.id != Buff.nauseous)
                return;

            if (buff.millisecondsDuration == Buff.ENDLESS || buff.millisecondsDuration > MaxNauseaDuration)
            {
                buff.millisecondsDuration = MaxNauseaDuration;
                buff.totalMillisecondsDuration = MaxNauseaDuration;
            }
        }
    }
}

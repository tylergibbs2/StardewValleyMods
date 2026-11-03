using HarmonyLib;
using StardewValley.Buffs;
using StardewValley.Objects;

namespace StardewRoguelike.Patches
{
    /// <summary>
    /// Swaps the defense and immunity the player's boots grant when they carry
    /// one of the boots curses.
    ///
    /// 1.5.6 needed this on <c>Boots.onEquip</c>/<c>onUnequip</c>, where the boots
    /// mutated the farmer's stats directly and had to be re-equipped for a change
    /// to take hold. In 1.6 equipment contributes through
    /// <see cref="Boots.AddEquipmentEffects"/>, which is recalculated whenever buffs
    /// change, so overriding the numbers there is enough.
    /// </summary>
    [HarmonyPatch(typeof(Boots), nameof(Boots.AddEquipmentEffects))]
    internal class CursedBootsPatch
    {
        public static void Postfix(Boots __instance, BuffEffects effects)
        {
            if (Curse.HasCurse(CurseType.BootsBetterDefense))
            {
                effects.Defense.Value += 10 - __instance.defenseBonus.Value;
                effects.Immunity.Value += -10 - __instance.immunityBonus.Value;
            }
            else if (Curse.HasCurse(CurseType.BootsBetterImmunity))
            {
                effects.Defense.Value += -10 - __instance.defenseBonus.Value;
                effects.Immunity.Value += 10 - __instance.immunityBonus.Value;
            }
        }
    }
}

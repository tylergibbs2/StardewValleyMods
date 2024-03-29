﻿using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Objects;
using System;
using System.Reflection;
using System.Text;

namespace StardewRoguelike.Patches
{
    [HarmonyPatch(typeof(Boots), "onEquip")]
    internal class CursedBootsEquipPatch
    {
        public static bool Prefix(Boots __instance)
        {
            bool hasCurse = false;
            if (Curse.HasCurse(CurseType.BootsBetterDefense))
            {
                __instance.defenseBonus.Value = 10;
                __instance.immunityBonus.Value = -10;
                hasCurse = true;
            }
            if (Curse.HasCurse(CurseType.BootsBetterImmunity))
            {
                __instance.defenseBonus.Value = -10;
                __instance.immunityBonus.Value = 10;
                hasCurse = true;
            }

            if (!hasCurse)
                __instance.reloadData();

            return true;
        }
    }

    [HarmonyPatch(typeof(Boots), "onUnequip")]
    internal class CursedBootsUnequipPatch
    {
        public static bool Prefix(Boots __instance)
        {
            bool hasCurse = false;
            if (Curse.HasCurse(CurseType.BootsBetterDefense))
            {
                __instance.defenseBonus.Value = 10;
                __instance.immunityBonus.Value = -10;
                hasCurse = true;
            }
            if (Curse.HasCurse(CurseType.BootsBetterImmunity))
            {
                __instance.defenseBonus.Value = -10;
                __instance.immunityBonus.Value = 10;
                hasCurse = true;
            }

            if (!hasCurse)
                __instance.reloadData();

            return true;
        }
    }
}

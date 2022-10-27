using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using System;

namespace StardewHitboxes.Patches
{
    [HarmonyPatch(typeof(GameLocation), "damageMonster", new Type[] { typeof(Rectangle), typeof(int), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(float), typeof(float), typeof(bool), typeof(Farmer) })]
    internal class GameLocationDamageMonster
    {
        public static void Postfix(Rectangle areaOfEffect)
        {
            ModEntry.RenderWeaponAOE(areaOfEffect);
        }
    }
}

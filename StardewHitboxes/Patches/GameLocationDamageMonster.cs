using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewValley;
using System;

namespace StardewHitboxes.Patches
{
    // 1.6 added a trailing isProjectile parameter; Harmony matches the signature exactly, so it has to be listed.
    [HarmonyPatch(typeof(GameLocation), "damageMonster", new Type[] { typeof(Rectangle), typeof(int), typeof(int), typeof(bool), typeof(float), typeof(int), typeof(float), typeof(float), typeof(bool), typeof(Farmer), typeof(bool) })]
    internal class GameLocationDamageMonster
    {
        public static void Postfix(Rectangle areaOfEffect)
        {
            ModEntry.RenderWeaponAOE(areaOfEffect);
        }
    }
}

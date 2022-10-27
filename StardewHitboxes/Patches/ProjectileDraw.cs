using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Projectiles;

namespace StardewHitboxes.Patches
{
    [HarmonyPatch(typeof(Projectile), "draw")]
    internal class ProjectileDraw
    {
        public static void Postfix(Projectile __instance, SpriteBatch b)
        {
            ModEntry.DrawHitbox(b, __instance);
        }
    }
}

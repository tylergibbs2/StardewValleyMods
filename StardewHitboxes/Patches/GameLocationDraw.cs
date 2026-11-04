using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.TerrainFeatures;

namespace StardewHitboxes.Patches
{
    [HarmonyPatch(typeof(GameLocation), "draw")]
    internal class GameLocationDraw
    {
        public static void Postfix(GameLocation __instance, SpriteBatch b)
        {
            foreach (Farmer farmer in __instance.farmers)
                ModEntry.DrawHitbox(b, farmer);

            foreach (Character character in __instance.characters)
                ModEntry.DrawHitbox(b, character);

            foreach (TerrainFeature terrainFeature in __instance.terrainFeatures.Values)
                ModEntry.DrawHitbox(b, terrainFeature);

            foreach (Object stardewObject in __instance.objects.Values)
                ModEntry.DrawHitbox(b, stardewObject);
        }
    }
}

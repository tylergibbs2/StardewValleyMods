using HarmonyLib;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace StardewNametags
{
    [HarmonyPatch(typeof(Farmer), "draw")]
    class DrawPlayerNames
    {
        public static void Postfix(Farmer __instance, SpriteBatch b)
        {
            if (ModEntry.DisplayNames)
                PlayerNameBox.draw(b, __instance);
        }
    }
}

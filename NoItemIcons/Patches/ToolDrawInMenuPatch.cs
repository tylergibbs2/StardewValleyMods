using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace NoItemIcons.Patches
{
    [HarmonyPatch(typeof(Tool), nameof(Tool.drawInMenu))]
    internal class ToolDrawInMenuPatch
    {
        public static bool Prefix(SpriteBatch spriteBatch, Vector2 location, float transparency, float layerDepth)
        {
            if (!ModEntry.IsActive)
                return true;

            ModEntry.DrawWhiteIcon(spriteBatch, location, transparency, layerDepth);

            return false;
        }
    }
}

using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Tools;

namespace NoItemIcons.Patches
{
    [HarmonyPatch(typeof(WateringCan), nameof(WateringCan.drawInMenu))]
    internal class WateringCanDrawInMenuPatch
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

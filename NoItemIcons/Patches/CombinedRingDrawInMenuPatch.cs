using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley.Objects;

namespace NoItemIcons.Patches
{
    [HarmonyPatch(typeof(CombinedRing), nameof(CombinedRing.drawInMenu))]
    internal class CombinedRingDrawInMenuPatch
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

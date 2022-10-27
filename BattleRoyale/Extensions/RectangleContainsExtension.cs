using Microsoft.Xna.Framework;

namespace BattleRoyale.Extensions
{
    public static class RectangleContainsExtension
    {
        public static bool Contains(this Rectangle bounds, DoorOrWarp warp)
        {
            return bounds.Contains(warp.Position);
        }
    }
}

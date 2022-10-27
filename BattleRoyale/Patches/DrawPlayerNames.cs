using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace BattleRoyale.Patches
{
    class DrawPlayerNames : Patch
    {
        protected override PatchDescriptor GetPatchDescriptor() => new(typeof(Farmer), "draw");

        public static void Postfix(Farmer __instance, SpriteBatch b)
        {
            Round round = ModEntry.BRGame.GetActiveRound();
            if (round != null && !round.AlivePlayers.Contains(__instance))
                return;

            if (ModEntry.DisplayNames)
                PlayerNameBox.draw(b, __instance);
        }
    }
}

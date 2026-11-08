using HarmonyLib;
using StardewRoguelike.Run;
using StardewRoguelike.UI;
using StardewValley;

namespace StardewRoguelike.Patches
{
    [HarmonyPatch(typeof(Game1), nameof(Game1.enterMine))]
    internal class EnterMinePatch
    {
        public static bool Prefix()
        {
            int next = Roguelike.CurrentLevel + 1;

            // Merchant and boss floors are forced, so taking the exit on the floor
            // before one of them descends as it always did. Everywhere else the exit
            // asks where to go first.
            if (!RunRoute.IsSettled(next))
            {
                Game1.activeClickableMenu = new ExitChoiceMenu(next);
                return false;
            }

            Roguelike.Descend();
            return false;
        }
    }
}

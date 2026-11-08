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
            // A menu with nothing on it would be a ladder that does nothing, so the
            // descent goes ahead instead. The plan always offers something; this is
            // here so a bug there cannot wedge the exit.
            if (!RunRoute.IsSettled(next) && RunRoute.OffersFor(next).Length > 0)
            {
                Game1.activeClickableMenu = new ExitChoiceMenu(next);
                return false;
            }

            Roguelike.Descend();
            return false;
        }
    }
}

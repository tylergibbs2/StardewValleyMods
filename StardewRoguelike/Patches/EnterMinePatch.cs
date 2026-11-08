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
            // The menu deliberately has no way out, since the floor has no other
            // exit - so it must never open with nothing to pick. The plan always
            // offers something; this is here so a bug there strands nobody.
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

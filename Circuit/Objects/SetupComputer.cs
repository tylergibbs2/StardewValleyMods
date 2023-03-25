using Circuit.UI;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;

namespace Circuit.Objects
{
    public class SetupComputer : SObject
    {
        public SetupComputer() : base() { }

        public SetupComputer(Vector2 tileLocation) : base(tileLocation, 239, false) { }

        public override bool checkForAction(Farmer who, bool justCheckingForActivity = false)
        {
            if (justCheckingForActivity && Context.IsMainPlayer)
                return true;

            if (!Context.IsMainPlayer)
            {
                Game1.drawObjectDialogue("Only the host can use this.");
                return false;
            }

            shakeTimer = 500;
            who.currentLocation.localSound("DwarvishSentry");
            who.freezePause = 500;
            DelayedAction.functionAfterDelay(delegate
            {
                Game1.activeClickableMenu = new GameSetupMenu();
            }, 500);

            return true;
        }

        public override bool performToolAction(Tool t, GameLocation location)
        {
            return false;
        }
    }
}

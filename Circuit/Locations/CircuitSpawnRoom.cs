using System;
using Circuit.UI;
using StardewValley;
using xTile.Dimensions;

namespace Circuit.Locations
{
    public class CircuitSpawnRoom : GameLocation
    {
        public CircuitSpawnRoom() : base() { }

        public CircuitSpawnRoom(string mapPath, string name) : base(mapPath, name) { }

        public override bool performAction(string action, Farmer who, Location tileLocation)
        {
            string[] actionParams = action.Split(' ');
            switch (actionParams[0])
            {
                case "TaskBoard":
                    Game1.activeClickableMenu = new TaskListMenu();
                    return true;
                default:
                    break;
            }

            return base.performAction(action, who, tileLocation);
        }
    }
}

using StardewRoguelike.UI;
using StardewValley;

namespace StardewRoguelike.Netcode
{
    internal class AttackIndicatorMessage
    {
        public string LocationName { get; set; } = null!;

        public int TickDuration { get; set; }

        public void Trigger()
        {
            if (Game1.player.currentLocation.Name == LocationName)
            {
                Game1.playSound("shadowpeep");
                Game1.onScreenMenus.Add(new AttackIndicator(TickDuration));
            }
        }
    }
}

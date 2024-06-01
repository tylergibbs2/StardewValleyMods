using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Characters;

namespace ColdPets
{
    public class ModEntry : Mod
    {
        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.DayStarted += OnDayStarted;
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            if (Game1.currentSeason != "winter" || !Context.IsMainPlayer)
                return;

            foreach (Pet pet in Utility.getAllPets())
            {
                pet.CurrentBehavior = Pet.behavior_SitDown;
                pet.warpToFarmHouse(Game1.player);
            }
        }
    }
}

using StardewModdingAPI;
using StardewValley;
using System;
using System.IO;
using System.Reflection;

namespace StardewRoguelike
{
    /// <summary>
    /// The save the mod ships with, which is not in the player's save folder.
    /// </summary>
    /// <remarks>
    /// Read from the mod's own directory, which is why it never appears in the load
    /// menu beside somebody's farms unless the mod puts it there.
    /// </remarks>
    internal static class BundledSave
    {
        private static string Directory =>
            Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                "assets", "Saves", Constants.SaveFile);

        public static string InfoPath => Path.Combine(Directory, "SaveGameInfo");

        public static string DataPath => Path.Combine(Directory, Constants.SaveFile);

        /// <summary>
        /// The farmer the save belongs to, for showing it in a menu.
        /// </summary>
        /// <remarks>
        /// Null rather than throwing where the file cannot be read. This is called
        /// while a menu is being built, and a mod with a missing asset should cost
        /// the player a row in a list, not the menu.
        /// </remarks>
        public static Farmer? ReadFarmer()
        {
            try
            {
                using FileStream stream = File.OpenRead(InfoPath);

                Farmer farmer = (Farmer)SaveGame.farmerSerializer.Deserialize(stream)!;
                SaveGame.loadDataToFarmer(farmer);
                farmer.slotName = Constants.SaveFile;

                return farmer;
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"Could not read the bundled save: {ex.Message}", LogLevel.Warn);
                return null;
            }
        }
    }
}

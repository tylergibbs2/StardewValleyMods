using StardewModdingAPI.Utilities;
using StardewRoguelike.UI;
using StardewRoguelike.VirtualProperties;
using StardewValley.Locations;
using System.Collections.Generic;

namespace StardewRoguelike
{
    public class ForgeFloor
    {
        public static readonly string ForgeFloorMapPath = "custom-forge";

        public static readonly int ForgeFloorMinimumLevel = 25;

        public static readonly int ForgeFloorMinimumInterval = 10;

        public static readonly double ForgeFloorChance = 0.15;

        private static readonly PerScreen<LimitedForgeMenu?> ScreenCurrentForge = new(() => null);

        /// <summary>The forge the player on this screen has open.</summary>
        public static LimitedForgeMenu? CurrentForge
        {
            get => ScreenCurrentForge.Value;
            set => ScreenCurrentForge.Value = value;
        }

        public static bool IsForgeFloor(MineShaft mine)
        {
            return mine.get_MineShaftForgeFloor().Value;
        }

        public static List<string> GetMusicTracks()
        {
            return new() { "Volcano_Ambient" };
        }
    }
}

using System.Collections.Generic;
using StardewValley;

namespace StardewRoguelike
{
    internal static class Constants
    {
        public static readonly string SaveFile = "Roguelike_311053312";

        public static readonly List<string> ValidMineMaps = new() {
            "2", "3", "4", "5", "6", "7",
            "8", "9", "11", "13", "15",
            "21", "23", "25", "26", "27",
            "custom-1", "custom-2", "custom-3",
            "custom-4", "custom-5", "custom-6",
            "custom-7", "custom-8", "custom-9"
        };

        public static readonly List<string> MapsWithWater = new()
        {
            "custom-1", "custom-2", "custom-3", "custom-8"
        };


        public static readonly List<int> ScalingOrder = new() { 6, 12, 18, 24, 30, 36, 42, 48 };
        public static readonly int DangerousThreshold = 24;

        public static readonly List<int> FloorsIncreaseGoldMax = new() { 6 };
        public static readonly List<int> FloorsIncreaseGoldMin = new() { 24 };

        internal readonly static string[] RandomDebuffIds = new string[]
        {
            Buff.goblinsCurse, Buff.tipsy, Buff.slimed,
            Buff.fear, Buff.evilEye, Buff.frozen,
            Buff.nauseous, Buff.darkness, Buff.weakness
        };

        internal readonly static string[] RandomBuffIds = new string[]
        {
            Buff.warriorEnergy, Buff.yobaBlessing, Buff.adrenalineRush,
            Buff.squidInkRavioli
        };

        public static readonly int MinimumMonstersPerFloor = 5;

        /// <summary>
        /// The same, on a floor whose shrine is the reward for clearing it.
        ///
        /// Higher because the floor has to be cleared to get past it, so this is the
        /// price of the perk the shrine trades. At the ordinary five a shrine floor
        /// was a mining floor with a free perk on it.
        /// </summary>
        public static readonly int MinimumMonstersPerShrineFloor = 10;
        public static readonly int MaximumMonstersPerFloorPreLoop = 15;
        public static readonly int MaximumMonstersPerFloorPostLoop = 30;

        public static readonly int MaximumPerkCount = 12;

        public static readonly List<int> PossibleFish = new() { 155, 269, 698, 795, 838, 128, 129 };
    }
}

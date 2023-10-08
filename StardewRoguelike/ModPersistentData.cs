using System.Collections.Generic;

namespace StardewRoguelike
{
    internal class ModPersistentData
    {
        public bool UnlockedBossArena { get; set; } = false;

        public List<Stats> RunHistory { get; set; } = new List<Stats>();
    }
}

﻿using StardewValley;
using StardewValley.Monsters;
using System.Collections.Generic;

namespace BattleRoyale.Patches
{
    class RemoveExtraMobDropsSerpent : Patch
    {
        protected override PatchDescriptor GetPatchDescriptor() => new(typeof(Serpent), "getExtraDropItems");

        public static bool Prefix(ref List<Item> __result)
        {
            __result = new List<Item>();
            return false;
        }
    }

    class RemoveExtraMobDropsMetalHead : Patch
    {
        protected override PatchDescriptor GetPatchDescriptor() => new(typeof(MetalHead), "getExtraDropItems");

        public static bool Prefix(ref List<Item> __result)
        {
            __result = new List<Item>();
            return false;
        }
    }


    class RemoveExtraMobDropsGreenSlime : Patch
    {
        protected override PatchDescriptor GetPatchDescriptor() => new(typeof(GreenSlime), "getExtraDropItems");

        public static bool Prefix(ref List<Item> __result)
        {
            __result = new List<Item>();
            return false;
        }
    }

    class RemoveExtraMobDropsBat : Patch
    {
        protected override PatchDescriptor GetPatchDescriptor() => new(typeof(Bat), "getExtraDropItems");

        public static bool Prefix(ref List<Item> __result)
        {
            __result = new List<Item>();
            return false;
        }
    }
}

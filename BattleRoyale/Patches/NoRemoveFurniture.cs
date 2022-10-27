using StardewValley;
using StardewValley.Objects;

namespace BattleRoyale.Patches
{
    class NoRemoveFurniture : Patch
    {
        protected override PatchDescriptor GetPatchDescriptor() => new(typeof(Furniture), "canBeRemoved");

        public static bool Prefix(ref bool __result)
        {
            __result = false;
            return false;
        }
    }

    class NoRemoveSeasonalPlants : Patch
    {
        private static readonly string[] preventRemoval = new string[]
        {
            "Seasonal",
            "Arcade System",
            "Campfire",
            "Barrel Brazier"
        };

        protected override PatchDescriptor GetPatchDescriptor() => new(typeof(Object), "performToolAction");

        public static bool Prefix(Object __instance, ref bool __result)
        {
            foreach (string item in preventRemoval)
            {
                if (__instance.name.Contains(item))
                {
                    __result = false;
                    return false;
                }
            }

            return true;
        }
    }
}

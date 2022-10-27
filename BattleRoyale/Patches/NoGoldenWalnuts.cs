using StardewValley.TerrainFeatures;

namespace BattleRoyale.Patches
{
    class NoGoldenWalnuts : Patch
    {
        protected override PatchDescriptor GetPatchDescriptor() => new(typeof(Bush), "shake");

        public static bool Prefix(Bush __instance)
        {
            if (__instance.size.Value == 4)
                return false;

            return true;
        }
    }
}

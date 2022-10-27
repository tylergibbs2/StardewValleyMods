using StardewValley;

namespace BattleRoyale.Patches
{
    class DisableAchievements : Patch
    {
        protected override PatchDescriptor GetPatchDescriptor() => new(typeof(Game1), "getAchievement");

        public static bool Prefix()
        {
            return false;
        }
    }
}

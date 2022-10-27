using StardewValley;

namespace BattleRoyale.Patches
{
    class DisableMines_enterMine : Patch
    {
        protected override PatchDescriptor GetPatchDescriptor() => new(typeof(Game1), "enterMine");
        public static bool Prefix()
        {
            return !ModEntry.BRGame.InProgress;
        }

    }

    class DisableMines_nextMineLevel : Patch
    {
        protected override PatchDescriptor GetPatchDescriptor() => new(typeof(Game1), "nextMineLevel");
        public static bool Prefix()
        {
            return !ModEntry.BRGame.InProgress;
        }
    }
}

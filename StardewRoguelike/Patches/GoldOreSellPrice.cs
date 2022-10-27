using StardewValley;

namespace StardewRoguelike.Patches
{
	internal class GoldOreSellPrice : Patch
	{
		protected override PatchDescriptor GetPatchDescriptor() => new(typeof(SObject), "sellToStorePrice");

		public static bool Prefix(SObject __instance, ref int __result)
		{
			if (__instance.ParentSheetIndex == 384)
            {
				__result = 15 * Game1.getOnlineFarmers().Count;
				return false;
            }

			return true;
		}
	}
}

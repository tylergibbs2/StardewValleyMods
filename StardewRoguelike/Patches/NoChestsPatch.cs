using HarmonyLib;
using StardewValley.Locations;

namespace StardewRoguelike.Patches
{
    [HarmonyPatch(typeof(MineShaft), "addLevelChests")]
	internal class NoChestsPatch
	{
		public static bool Prefix()
		{
			return false;
		}
	}
}

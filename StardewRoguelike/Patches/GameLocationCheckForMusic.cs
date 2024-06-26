using HarmonyLib;
using StardewValley;
using StardewValley.Locations;

namespace StardewRoguelike.Patches
{
    [HarmonyPatch(typeof(GameLocation), nameof(GameLocation.checkForMusic))]
    internal class GameLocationCheckForMusic
    {
        public static bool Prefix(GameLocation __instance)
        {
            if (__instance is MineShaft)
                return true;
            else if (__instance is not Mine)
                return true;

            string targetTrack = "Upper_Ambient";
            if (!Game1.currentSong.IsPlaying)
                Game1.changeMusicTrack("none");

            if (targetTrack != Game1.getMusicTrackName())
                Game1.changeMusicTrack(targetTrack);

            return false;
        }
    }
}

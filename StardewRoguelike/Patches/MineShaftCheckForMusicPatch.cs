using HarmonyLib;
using StardewRoguelike.UI;
using StardewValley;
using StardewValley.Locations;

namespace StardewRoguelike.Patches
{
    [HarmonyPatch(typeof(MineShaft), nameof(MineShaft.checkForMusic))]
    internal class MineShaftCheckForMusicPatch
    {
        internal static bool ShouldAnnounceMusic { get; set; } = false;

        public static bool Prefix(MineShaft __instance)
        {
            string? targetTrack;
            if (Merchant.IsMerchantFloor(__instance))
            {
                if (Merchant.GetMusicTracks().Contains(Game1.getMusicTrackName()))
                    targetTrack = Game1.getMusicTrackName();
                else
                    targetTrack = Roguelike.GetRandomTrack(Merchant.GetMusicTracks());
            }
            else if (ForgeFloor.IsForgeFloor(__instance))
                targetTrack = Roguelike.GetRandomTrack(ForgeFloor.GetMusicTracks());
            else if (BossFloor.IsBossFloor(__instance))
            {
                if (BossFloor.GetMusicTracks(__instance).Contains(Game1.getMusicTrackName()))
                    targetTrack = Game1.getMusicTrackName();
                else
                    targetTrack = Roguelike.GetRandomTrack(BossFloor.GetMusicTracks(__instance));
            }
            else if (ChallengeFloor.IsChallengeFloor(__instance) && ChallengeFloor.GetMusicTracks(__instance) is not null)
            {
                if (ChallengeFloor.GetMusicTracks(__instance).Contains(Game1.getMusicTrackName()))
                    targetTrack = Game1.getMusicTrackName();
                else
                    targetTrack = Roguelike.GetRandomTrack(ChallengeFloor.GetMusicTracks(__instance));
            }
            else
            {
                if (Game1.getMusicTrackName() != __instance.getMineSong())
                    targetTrack = __instance.getMineSong();
                else
                    targetTrack = Game1.getMusicTrackName();
            }

            // currentSong is null whenever nothing is playing, which is precisely what
            // the line below leaves behind: changeMusicTrack("none") stops the cue and
            // clears it. Reading IsPlaying off it threw, so the first time this took
            // that branch it broke itself for the rest of the run - the throw happens
            // before the track is ever changed, and checkForMusic is called from
            // Game1.UpdateOther, so it threw out of the update loop every frame after.
            //
            // BossManager.Death walks straight into it: it silences the music and
            // calls checkForMusic on the next line, so a boss kill set this off every
            // time, and the reward chest it spawns afterwards was never reached.
            bool playing = Game1.currentSong?.IsPlaying ?? false;

            if (!playing || string.IsNullOrEmpty(targetTrack))
                Game1.changeMusicTrack("none");

            if (targetTrack != Game1.getMusicTrackName() || Game1.getMusicTrackName().EndsWith("_Ambient"))
            {
                Game1.changeMusicTrack(targetTrack);
                if (ShouldAnnounceMusic)
                {
                    string credits = Roguelike.GetMusicCredits(Game1.getMusicTrackName());
                    if (!string.IsNullOrEmpty(credits))
                    {
                        Game1.onScreenMenus.Add(
                            new MusicAnnounceMenu(credits)
                        );
                    }
                    ShouldAnnounceMusic = false;
                }
            }

            return false;
        }
    }
}

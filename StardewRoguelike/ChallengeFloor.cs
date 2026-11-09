using StardewValley;
using StardewValley.Locations;
using StardewRoguelike.VirtualProperties;
using System;
using Microsoft.Xna.Framework;
using StardewRoguelike.ChallengeFloors;
using System.Collections.Generic;
using StardewModdingAPI.Events;
using System.Linq;
using xTile.Dimensions;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;

namespace StardewRoguelike
{
    public class ChallengeFloor
    {
        #pragma warning disable format
        public enum ChallengeType
        {
            KOTH         = 1,
            TimedKills   = 2,
            HotSpring    = 3,
            DwarfChests  = 4,
            Race         = 5,
            SlingshotAim = 6,
            EggHunt      = 7,
            JOTPK        = 8,
            HiddenBoss   = 9,
            PickAPath    = 10,
            Speedrun     = 11,
            GambaChests  = 12,
            Horde        = 13,
            Quarry       = 14,
            Countdown    = 15
        }
        #pragma warning restore format

        /// <summary>
        /// Finds a MineShaft with the specified level.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <returns>The found MineShaft, if it exists. Otherwise null.</returns>
        public static MineShaft? GetMineFromLevel(int level)
        {
            foreach (MineShaft mine in MineShaft.activeMines)
            {
                if (Roguelike.GetLevelFromMineshaft(mine) == level)
                    return mine;
            }

            return null;
        }

        /// <summary>
        /// Calculates the number floors until the next boss appears, starting
        /// from the specified level.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <returns>The number of floors until the next boss.</returns>
        public static int FloorsUntilNextBoss(int level)
        {
            int count = 0;

            while (!BossFloor.IsBossFloor(level + count))
                count++;

            return count;
        }

        /// <summary>
        /// Checks whether or not a specified MineShaft is a challenge floor.
        /// This data is stored internally on MineShaft instances.
        /// </summary>
        /// <param name="mine">The mine.</param>
        /// <returns>true if it is, false otherwise.</returns>
        public static bool IsChallengeFloor(MineShaft mine)
        {
            return mine.get_MineShaftIsChallengeFloor().Value;
        }

        /// <summary>
        /// Gets the map path to load for a specified MineShaft.
        /// If the type of challenge that the MineShaft is doesn't
        /// have any map paths specified, load one of the default maps.
        /// </summary>
        /// <param name="mine">The mine.</param>
        /// <returns>The map path to load.</returns>
        public static string GetMapPath(MineShaft mine)
        {
            ChallengeBase challenge = mine.get_MineShaftChallengeFloor().Value;

            return
                challenge.MapPaths?[Roguelike.FloorRng.Next(challenge.MapPaths.Count)] ??
                Constants.ValidMineMaps[Roguelike.FloorRng.Next(Constants.ValidMineMaps.Count)];
        }

        /// <summary>
        /// Gets the music tracks for a specified MineShaft.
        /// </summary>
        /// <param name="mine">The mine.</param>
        /// <returns>A list of valid music tracks to play.</returns>
        public static List<string> GetMusicTracks(MineShaft mine)
        {
            ChallengeBase challenge = mine.get_MineShaftChallengeFloor().Value;
            return challenge.GetMusicTracks(mine);
        }

        /// <summary>
        /// Finds where the spawn location is for a specified MineShaft.
        /// If a spawn location isn't specified in the ChallengeBase,
        /// use the game default.
        /// </summary>
        /// <param name="mine">The mine.</param>
        /// <returns>The spawn tile location.</returns>
        public static Vector2 GetSpawnLocation(MineShaft mine)
        {
            Vector2? tileBeneathLadder = mine.tileBeneathLadder;

            ChallengeBase challenge = mine.get_MineShaftChallengeFloor().Value;
            return challenge.GetSpawnLocation(mine) ?? tileBeneathLadder ?? new(0, 0);
        }

        /// <summary>Builds the challenge floor for <paramref name="type"/>.</summary>
        /// <remarks>
        /// Split out of GetRandomChallenge so the run plan can name a challenge
        /// without going through the roll that used to pick one.
        /// </remarks>
        public static ChallengeBase Create(ChallengeType type)
        {
            return type switch
            {
                ChallengeType.KOTH => new KOTH(),
                ChallengeType.TimedKills => new TimedKills(),
                ChallengeType.HotSpring => new HotSpring(),
                ChallengeType.DwarfChests => new DwarfChests(),
                ChallengeType.Race => new Race(),
                ChallengeType.SlingshotAim => new SlingshotAim(),
                ChallengeType.EggHunt => new EggHunt(),
                ChallengeType.JOTPK => new JOTPK(),
                ChallengeType.HiddenBoss => new HiddenBoss(),
                ChallengeType.PickAPath => new PickAPath(),
                ChallengeType.Speedrun => new Speedrun(),
                ChallengeType.GambaChests => new GambaChests(),
                ChallengeType.Horde => new Horde(),
                ChallengeType.Quarry => new Quarry(),
                ChallengeType.Countdown => new Countdown(),
                _ => throw new NotImplementedException()
            };
        }

        /// <summary>
        /// Event handler for when the local player warps.
        /// This event is passed down to the ChallengeBase instance.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="WarpedEventArgs"/> instance containing the event data.</param>
        public static void PlayerWarped(object? sender, WarpedEventArgs e)
        {
            if (!e.IsLocalPlayer)
                return;

            if (e.OldLocation is MineShaft && IsChallengeFloor((MineShaft)e.OldLocation))
            {
                ChallengeBase challenge = ((MineShaft)e.OldLocation).get_MineShaftChallengeFloor().Value;
                challenge.PlayerLeft((MineShaft)e.OldLocation);
            }
            if (e.NewLocation is MineShaft && IsChallengeFloor((MineShaft)e.NewLocation))
            {
                ChallengeBase challenge = ((MineShaft)e.NewLocation).get_MineShaftChallengeFloor().Value;
                challenge.PlayerEntered((MineShaft)e.NewLocation);
            }
        }

        /// <summary>
        /// Event handler for when dialogue is answered by the local player.
        /// This event is passed down to the ChallengeBase instance.
        /// </summary>
        /// <param name="mine">The mine.</param>
        /// <param name="questionAndAnswer">The question and answer.</param>
        /// <param name="questionParams">The question parameters.</param>
        /// <returns></returns>
        public static bool AnswerDialogueAction(MineShaft mine, string questionAndAnswer, string[] questionParams)
        {
            ChallengeBase challenge = mine.get_MineShaftChallengeFloor().Value;
            if (challenge is null)
                return false;

            return challenge.AnswerDialogueAction(mine, questionAndAnswer, questionParams);
        }

        public static void DrawBeforeLocation(MineShaft mine, SpriteBatch b)
        {
            ChallengeBase challenge = mine.get_MineShaftChallengeFloor().Value;
            if (challenge is null)
                return;

            challenge.DrawBeforeLocation(mine, b);
        }

        public static void DrawAfterLocation(MineShaft mine, SpriteBatch b)
        {
            ChallengeBase challenge = mine.get_MineShaftChallengeFloor().Value;
            if (challenge is null)
                return;

            challenge.DrawAfterLocation(mine, b);
        }

        public static bool CheckForCollision(MineShaft mine, Microsoft.Xna.Framework.Rectangle position, Farmer who)
        {
            ChallengeBase challenge = mine.get_MineShaftChallengeFloor().Value;
            if (challenge is null)
                return false;

            return challenge.CheckForCollision(mine, position, who);
        }

        public static bool CheckAction(MineShaft mine, Location tileLocation, xTile.Dimensions.Rectangle viewport, Farmer who)
        {
            ChallengeBase challenge = mine.get_MineShaftChallengeFloor().Value;
            if (challenge is null)
                return false;

            return challenge.CheckAction(mine, tileLocation, viewport, who);
        }

        public static bool PerformAction(MineShaft mine, string action, Farmer who, Location tileLocation)
        {
            ChallengeBase challenge = mine.get_MineShaftChallengeFloor().Value;
            if (challenge is null)
                return false;

            return challenge.PerformAction(mine, action, who, tileLocation);
        }

        /// <summary>
        /// Event handler for every update ticked.
        /// This event is passed down to the ChallengeBase instance.
        /// </summary>
        /// <param name="mine">The mine.</param>
        /// <param name="time">The time.</param>
        public static void DoUpdate(MineShaft mine, GameTime time)
        {
            if (!IsChallengeFloor(mine))
                return;

            ChallengeBase challenge = mine.get_MineShaftChallengeFloor().Value;
            challenge.Update(mine, time);
        }
    }
}

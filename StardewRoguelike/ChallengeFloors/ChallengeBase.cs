using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Locations;
using System;
using System.Collections.Generic;
using xTile.Dimensions;

namespace StardewRoguelike.ChallengeFloors
{
    public class ChallengeBase : INetObject<NetFields>, IDisposable
    {
        protected ChallengeBase()
        {
            NetFields = new(NetFields.GetNameForInstance(this));

            // Claim ownership before any field is registered. 1.6 uses the owner to
            // name fields in network error logs, and warns for every field added
            // without one - which was every field on every challenge floor.
            NetFields.SetOwner(this);
            InitNetFields();
        }

        public NetFields NetFields { get; }

        /// <summary>Whether a floor's challenge is still running or has been settled.</summary>
        public enum ChallengeState
        {
            /// <summary>Still being set up, or still being played.</summary>
            Running,

            /// <summary>Settled, won or lost. Whatever the floor gates on this is open.</summary>
            Over
        }

        private readonly NetEnum<ChallengeState> challengeState = new(ChallengeState.Running);

        /// <summary>
        /// Whether the challenge has been settled.
        ///
        /// Every floor that gates something on being finished used to keep its own
        /// answer to this, and they all disagreed about how: three plain bools, a
        /// NetBool, a NetString compared against "over", and one called
        /// canSpawnLadder. The plain ones were invisible to farmhands, which
        /// ShouldSpawnLadder is asked on, and JOTPK carries a comment about the bug
        /// that caused. One field, net-synced, so everyone reads the same answer.
        /// </summary>
        protected bool IsOver => challengeState.Value == ChallengeState.Over;

        /// <summary>
        /// Marks the challenge settled. Called by the host, which is the only side
        /// that runs the Update loops deciding a floor is won or lost.
        /// </summary>
        protected void Finish() => challengeState.Value = ChallengeState.Over;

        protected virtual void InitNetFields()
        {
            NetFields.AddField(challengeState);
        }

        public virtual List<string> MapPaths { get; } = null!;

        public virtual List<string> GetMusicTracks(MineShaft mine)
        {
            return null!;
        }

        public virtual Vector2? GetSpawnLocation(MineShaft mine)
        {
            return null;
        }

        // Runs only for the main player
        public virtual void Initialize(MineShaft mine) { }

        // Runs for all players
        public virtual void Update(MineShaft mine, GameTime time) { }

        // Runs only for Game1.player
        public virtual void PlayerEntered(MineShaft mine)
        {
            ModEntry.Events.GameLoop.ReturnedToTitle += DisposeHandler;
        }

        // Runs only for Game1.player
        public virtual void PlayerLeft(MineShaft mine)
        {
            ModEntry.Events.GameLoop.ReturnedToTitle -= DisposeHandler;
        }

        public virtual bool AnswerDialogueAction(MineShaft mine, string questionAndAnswer, string[] questionParams)
        {
            return false;
        }

        public virtual void DrawBeforeLocation(MineShaft mine, SpriteBatch b) { }

        public virtual void DrawAfterLocation(MineShaft mine, SpriteBatch b) { }

        public virtual bool CheckForCollision(MineShaft mine, Microsoft.Xna.Framework.Rectangle position, Farmer who)
        {
            return false;
        }

        public virtual bool CheckAction(MineShaft mine, Location tileLocation, xTile.Dimensions.Rectangle viewport, Farmer who)
        {
            return false;
        }

        public virtual bool PerformAction(MineShaft mine, string action, Farmer who, Location tileLocation)
        {
            return false;
        }

        public virtual bool ShouldSpawnLadder(MineShaft mine)
        {
            return true;
        }

        /// <summary>A tile beside <paramref name="origin"/> that a chest will fit on.</summary>
        /// <remarks>
        /// Falls back to the origin itself rather than to zero. Three floors each kept
        /// their own copy of this walk and two of them started the fallback at
        /// Vector2.Zero, which put the reward in the top-left corner of the map where
        /// nobody could reach it, so a floor could be won with nothing to show for it.
        ///
        /// Searched in rings rather than over the whole three-by-three block at once,
        /// so a chest lands against the ladder where the player is looking before it
        /// lands two tiles out.
        /// </remarks>
        protected static Vector2 RewardSpot(MineShaft mine, Vector2 origin, int maxRings = 4)
        {
            for (int ring = 1; ring <= maxRings; ring++)
            {
                for (int i = -ring; i <= ring; i++)
                {
                    for (int j = -ring; j <= ring; j++)
                    {
                        // Only the edge of this ring; the inside was already walked.
                        if (Math.Abs(i) != ring && Math.Abs(j) != ring)
                            continue;

                        Vector2 candidate = new((int)origin.X + i, (int)origin.Y + j);

                        if (mine.CanItemBePlacedHere(candidate) && !mine.overlayObjects.ContainsKey(candidate))
                            return candidate;
                    }
                }
            }

            return origin;
        }

        /// <summary>Draws one of the HUD readouts the timed floors stack down the left.</summary>
        /// <remarks>
        /// Takes the slot rather than a position so that a floor showing three
        /// readings does not have to measure the two above to place the third, which
        /// is what the hand-added text heights in the older floors were doing.
        /// </remarks>
        protected static void DrawHudReading(SpriteBatch b, int slot, string text)
        {
            Vector2 size = Game1.smallFont.MeasureString(text);
            Point at = new(100, 16 + slot * ((int)size.Y + 24));

            StardewValley.Menus.IClickableMenu.drawTextureBox(
                b, at.X - 15, at.Y - 12, (int)size.X + 33, (int)size.Y + 20, Color.White);

            Utility.drawTextWithShadow(b, text, Game1.smallFont, new Vector2(at.X, at.Y), Color.Black);
        }

        private void DisposeHandler(object? sender, ReturnedToTitleEventArgs e)
        {
            Dispose();
            ModEntry.Events.GameLoop.ReturnedToTitle -= DisposeHandler;
        }

        public virtual void Dispose() { }
    }
}

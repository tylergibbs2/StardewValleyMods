using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewRoguelike.Mode;
using StardewValley;
using StardewValley.Menus;

namespace StardewRoguelike.UI
{
    /// <summary>
    /// The roguelike's own row in the co-op menu's host list.
    /// </summary>
    /// <remarks>
    /// Hosting a run was reachable once: the save list was replaced with the single
    /// bundled save while the mode was on, so the host tab listed it and nothing
    /// else. The title-screen button turns the mode on and loads in the same breath,
    /// so by the time anybody opens the co-op menu the mode is off again and the
    /// list is the player's own farms. The run has had no host since.
    ///
    /// This puts it back as a choice rather than by taking the farms away, which is
    /// what the mode-scoped list replacement did. Both kinds of co-op are on the one
    /// tab, and this row has to be obviously not a farm.
    ///
    /// Vanilla's own HostFileSlot would do the hosting but is protected inside
    /// CoopMenu and cannot be drawn differently, so the two useful lines of it - the
    /// multiplayer flag and the name - are reimplemented here.
    /// </remarks>
    internal class RoguelikeHostSlot : LoadGameMenu.SaveFileSlot
    {
        /// <summary>The Wicked Statue, as the shrine draws it.</summary>
        private const int WickedStatue = 83;

        /// <summary>Hosting, rather than a local split-screen game.</summary>
        private const byte HostingMultiplayer = 2;

        public RoguelikeHostSlot(LoadGameMenu menu, Farmer farmer)
            : base(menu, farmer, null) { }

        /// <summary>
        /// Turns the mode on, then hosts.
        /// </summary>
        /// <remarks>
        /// The mode has to be on before the save is read, or the game loads the real
        /// mine instead of the lobby. Applied here rather than deferred to the update
        /// loop for the same reason the co-op handshake applies it immediately: the
        /// load begins on the next line.
        ///
        /// The multiplayer flag is what the load path reads to decide whether to
        /// start a server at all - without it this would be an ordinary solo load,
        /// which is exactly what the title button does.
        /// </remarks>
        public override void Activate()
        {
            // Before the mode changes anything, while the window is still as the
            // player set it - the bundled save carries its own zoom and scale.
            RoguelikeEntry.CaptureDisplay();

            RoguelikeMode.Apply(true);
            Game1.multiplayerMode = HostingMultiplayer;

            base.Activate();
        }

        /// <summary>
        /// Draws the statue where a save's number would go.
        /// </summary>
        /// <remarks>
        /// Host rows have no number - vanilla overrides it away - so this column is
        /// empty on every other row and the badge collides with nothing. The statue
        /// is the mod's most recognisable mark and the shrine already draws it, so
        /// it costs no new art.
        /// </remarks>
        protected override void drawSlotSaveNumber(SpriteBatch b, int i)
        {
            Rectangle bounds = menu.slotButtons[i].bounds;

            b.Draw(
                Game1.bigCraftableSpriteSheet,
                new Vector2(bounds.X + 24, bounds.Y + 16),
                StardewValley.Object.getSourceRectForBigCraftable(WickedStatue),
                Color.White,
                0f,
                Vector2.Zero,
                2f,
                SpriteEffects.None,
                1f);
        }

        protected override string slotName() => I18n.UI_HostSlot_Name();

        protected override string slotSubName() => I18n.UI_HostSlot_SubName();

        /// <summary>
        /// The three readings a farm has and a run does not.
        /// </summary>
        /// <remarks>
        /// A date, a balance and an hours-played, all read off the bundled save - so
        /// they are the same for every player, never change, and between them make
        /// the row look like somebody's farm with a history behind it. That is the
        /// opposite of what this row is for, so it does not draw them.
        /// </remarks>
        protected override void drawSlotDate(SpriteBatch b, int i) { }

        protected override void drawSlotMoney(SpriteBatch b, int i) { }

        protected override void drawSlotTimer(SpriteBatch b, int i) { }
    }
}

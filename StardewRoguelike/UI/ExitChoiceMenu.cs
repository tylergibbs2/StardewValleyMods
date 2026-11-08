using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewRoguelike.Run;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using System.Collections.Generic;

namespace StardewRoguelike.UI
{
    /// <summary>
    /// The choice of where to go next, shown when the player takes the exit on a
    /// floor the group has not settled yet.
    ///
    /// Replaces the hidden ladder as the thing a floor is "for": the question stops
    /// being where the way down is and becomes which way down to take.
    /// </summary>
    public class ExitChoiceMenu : IClickableMenu
    {
        private const int OptionHeight = 112;
        private const int OptionGap = 16;

        /// <summary>Where the icon sits inside an option, and where its text starts.</summary>
        private const int IconInset = 24;
        private const int TextInset = 96;

        /// <summary>Where the title scroll's text sits, relative to the menu top.</summary>
        private const int TitleTop = 24;

        /// <summary>
        /// The title scroll's art, in drawn pixels.
        ///
        /// SpriteText.drawString's scrollType 0 draws a fixed 18-pixel-tall scroll at
        /// 4x, starting 12 pixels above the text it wraps. getHeightOfString measures
        /// the text rather than the art, so a header sized from it comes up short and
        /// puts whatever follows inside the scroll.
        /// </summary>
        private const int ScrollHeight = 18 * 4;
        private const int ScrollOverhang = 12;

        /// <summary>The line under the title, clear of the scroll's lower edge.</summary>
        private const int SubtitleTop = TitleTop - ScrollOverhang + ScrollHeight + 12;

        /// <summary>Where the options begin.</summary>
        private const int HeaderHeight = SubtitleTop + 40;

        private readonly int level;
        private readonly FloorKind[] offers;
        private readonly List<ClickableComponent> options = new();

        /// <summary>Set once a choice is made, so Update can close and descend.</summary>
        private bool committed;

        public ExitChoiceMenu(int level) : base(0, 0, 0, 0, showUpperRightCloseButton: false)
        {
            this.level = level;
            offers = RunPlan.OffersFor(level);

            width = 800;
            height = HeaderHeight + offers.Length * (OptionHeight + OptionGap) - OptionGap + 64;

            SetUpPositions();
        }

        private void SetUpPositions()
        {
            xPositionOnScreen = Game1.uiViewport.Width / 2 - width / 2;
            yPositionOnScreen = Game1.uiViewport.Height / 2 - height / 2;

            options.Clear();
            for (int i = 0; i < offers.Length; i++)
            {
                options.Add(new ClickableComponent(
                    new Rectangle(
                        xPositionOnScreen + 32,
                        yPositionOnScreen + HeaderHeight + i * (OptionHeight + OptionGap),
                        width - 64,
                        OptionHeight),
                    offers[i].ToString())
                {
                    myID = i,
                    upNeighborID = i - 1,
                    downNeighborID = i < offers.Length - 1 ? i + 1 : -1
                });
            }
        }

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            base.gameWindowSizeChanged(oldBounds, newBounds);
            SetUpPositions();
        }

        public override void snapToDefaultClickableComponent()
        {
            currentlySnappedComponent = options.Count > 0 ? options[0] : null;
            snapCursorToCurrentSnappedComponent();
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (committed)
                return;

            for (int i = 0; i < options.Count; i++)
            {
                if (!options[i].containsPoint(x, y))
                    continue;

                Game1.playSound("smallSelect");
                RunRoute.Choose(level, offers[i]);
                committed = true;
                return;
            }
        }

        public override void receiveKeyPress(Keys key)
        {
            // Deliberately not closable: the floor has no other way out, so letting
            // the menu be dismissed would strand the player on a cleared floor.
            if (Game1.options.doesInputListContain(Game1.options.menuButton, key))
                return;

            base.receiveKeyPress(key);
        }

        public override void update(GameTime time)
        {
            base.update(time);

            if (!committed)
                return;

            // On the host the choice lands immediately. A farmhand's goes to the host
            // as a message, so wait for the team field to come back before descending
            // - otherwise the floor would be generated against an unsettled level.
            if (!RunRoute.IsSettled(level))
                return;

            exitThisMenuNoSound();
            Roguelike.DescendPending = true;
        }

        /// <summary>
        /// Draws text, shrinking it if it would not fit in <paramref name="room"/>.
        ///
        /// English fits at full size; this is for the translations, where the same
        /// line can be half again as long and would otherwise run off the option.
        /// </summary>
        private static void DrawFitted(SpriteBatch b, string text, SpriteFont font, Vector2 at, int room, Color color)
        {
            float measured = font.MeasureString(text).X;
            float scale = measured > room ? room / measured : 1f;

            Utility.drawTextWithShadow(b, text, font, at, color, scale);
        }

        public override void draw(SpriteBatch b)
        {
            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.6f);

            drawTextureBox(
                b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                xPositionOnScreen, yPositionOnScreen, width, height, Color.White);

            SpriteText.drawStringWithScrollCenteredAt(
                b, I18n.ExitChoice_Title(), xPositionOnScreen + width / 2, yPositionOnScreen + TitleTop);

            // Forward context, so the choice is made against something. Without it
            // every option is judged only on this floor, which is how the run reads
            // as a sequence of unrelated rooms rather than a route.
            int toBoss = ChallengeFloor.FloorsUntilNextBoss(level);
            string horizon = toBoss <= 1 ? I18n.ExitChoice_BossNext() : I18n.ExitChoice_BossIn(count: toBoss);
            Vector2 horizonSize = Game1.smallFont.MeasureString(horizon);
            Utility.drawTextWithShadow(
                b, horizon, Game1.smallFont,
                new Vector2(
                    xPositionOnScreen + width / 2 - horizonSize.X / 2,
                    yPositionOnScreen + SubtitleTop),
                Game1.textColor * 0.75f);

            for (int i = 0; i < options.Count; i++)
            {
                ClickableComponent option = options[i];
                bool hovered = option.containsPoint(Game1.getMouseX(), Game1.getMouseY());

                drawTextureBox(
                    b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                    option.bounds.X, option.bounds.Y, option.bounds.Width, option.bounds.Height,
                    hovered && !committed ? Color.Wheat : Color.White);

                FloorKind kind = offers[i];
                int textRoom = option.bounds.Width - TextInset - 24;

                DrawFitted(b, FloorKindInfo.Name(kind), Game1.dialogueFont,
                    new Vector2(option.bounds.X + TextInset, option.bounds.Y + 20),
                    textRoom, Game1.textColor);

                DrawFitted(b, FloorKindInfo.Description(kind), Game1.smallFont,
                    new Vector2(option.bounds.X + TextInset, option.bounds.Y + 60),
                    textRoom, Game1.textColor * 0.8f);

                Rectangle icon = FloorKindInfo.Icon(kind);
                b.Draw(
                    Game1.mouseCursors,
                    new Vector2(option.bounds.X + IconInset, option.bounds.Y + 24),
                    icon, Color.White, 0f, Vector2.Zero, 56f / icon.Width, SpriteEffects.None, 1f);
            }

            if (committed)
            {
                Utility.drawTextWithShadow(
                    b, I18n.ExitChoice_Waiting(), Game1.smallFont,
                    new Vector2(xPositionOnScreen + 32, yPositionOnScreen + height - 56), Game1.textColor);
            }

            drawMouse(b);
        }
    }
}

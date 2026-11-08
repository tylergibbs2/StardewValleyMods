using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewRoguelike.Run;
using StardewValley;
using StardewValley.BellsAndWhistles;
using StardewValley.Menus;
using System;
using System.Collections.Generic;

namespace StardewRoguelike.UI
{
    /// <summary>
    /// The choice of where to go next, shown when the player takes the exit on a
    /// floor the group has not settled yet.
    ///
    /// Replaces the hidden ladder as the thing a floor is "for": the question stops
    /// being where the way down is and becomes which way down to take.
    ///
    /// Closing it is allowed. The ladder stays where it is, so backing out costs
    /// nothing but the walk back, and the offers are worked out from the seed and the
    /// route rather than rolled when the menu opens - the same three are waiting.
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

        /// <summary>How long the menu takes to settle into place, in milliseconds.</summary>
        private const float OpenDuration = 200f;

        /// <summary>How far above its resting place the menu starts.</summary>
        private const float OpenRise = 40f;

        /// <summary>Milliseconds since the menu opened, capped at <see cref="OpenDuration"/>.</summary>
        private float opened;

        /// <summary>How far through the opening the menu is, eased, from 0 to 1.</summary>
        private float Entrance
        {
            get
            {
                float t = opened / OpenDuration;
                return 1f - (1f - t) * (1f - t) * (1f - t);
            }
        }

        public ExitChoiceMenu(int level) : base(0, 0, 0, 0, showUpperRightCloseButton: true)
        {
            this.level = level;
            offers = RunRoute.OffersFor(level);

            width = 800;
            height = HeaderHeight + offers.Length * (OptionHeight + OptionGap) - OptionGap + 64;

            SetUpPositions();

            Game1.playSound("bigSelect");
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

            // Built from the final bounds rather than the empty ones the base
            // constructor was given, which is where it would otherwise sit.
            initializeUpperRightCloseButton();
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
            // Nothing is clickable until it has stopped moving, so a click always
            // lands on the option it looked like it would.
            if (committed || opened < OpenDuration)
                return;

            if (upperRightCloseButton is not null && upperRightCloseButton.containsPoint(x, y))
            {
                exitThisMenu();
                return;
            }

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

        public override void performHoverAction(int x, int y)
        {
            base.performHoverAction(x, y);

            if (upperRightCloseButton is not null)
                upperRightCloseButton.tryHover(x, y);
        }

        public override void receiveKeyPress(Keys key)
        {
            bool cancel = key == Keys.Escape
                          || Game1.options.doesInputListContain(Game1.options.menuButton, key);

            // Backing out leaves the ladder where it is, so the player can keep
            // playing the floor and take the same three offers whenever they like.
            if (cancel && !committed && readyToClose())
            {
                exitThisMenu();
                return;
            }

            base.receiveKeyPress(key);
        }

        public override void update(GameTime time)
        {
            base.update(time);

            if (opened < OpenDuration)
                opened = Math.Min(OpenDuration, opened + (float)time.ElapsedGameTime.TotalMilliseconds);

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
            // The whole menu drops the last few pixels into place and fades up as it
            // does, so an exit that is now a question announces itself rather than
            // appearing between one frame and the next.
            float entrance = Entrance;
            float alpha = entrance;
            int rise = (int)((1f - entrance) * -OpenRise);
            Color tint = Color.White * alpha;

            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * (0.6f * alpha));

            drawTextureBox(
                b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                xPositionOnScreen, yPositionOnScreen + rise, width, height, tint);

            SpriteText.drawStringWithScrollCenteredAt(
                b, I18n.ExitChoice_Title(), xPositionOnScreen + width / 2,
                yPositionOnScreen + TitleTop + rise, "", alpha);

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
                    yPositionOnScreen + SubtitleTop + rise),
                Game1.textColor * (0.75f * alpha));

            for (int i = 0; i < options.Count; i++)
            {
                ClickableComponent option = options[i];
                bool hovered = entrance >= 1f
                               && option.containsPoint(Game1.getMouseX(), Game1.getMouseY());
                int top = option.bounds.Y + rise;

                drawTextureBox(
                    b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                    option.bounds.X, top, option.bounds.Width, option.bounds.Height,
                    (hovered && !committed ? Color.Wheat : Color.White) * alpha);

                FloorKind kind = offers[i];
                int textRoom = option.bounds.Width - TextInset - 24;

                DrawFitted(b, FloorKindInfo.Name(kind), Game1.dialogueFont,
                    new Vector2(option.bounds.X + TextInset, top + 20),
                    textRoom, Game1.textColor * alpha);

                DrawFitted(b, FloorKindInfo.Description(kind), Game1.smallFont,
                    new Vector2(option.bounds.X + TextInset, top + 60),
                    textRoom, Game1.textColor * (0.8f * alpha));

                Rectangle icon = FloorKindInfo.Icon(kind);
                b.Draw(
                    FloorKindInfo.IconTexture(kind),
                    new Vector2(option.bounds.X + IconInset, top + 24),
                    icon, tint, 0f, Vector2.Zero, 56f / icon.Width, SpriteEffects.None, 1f);
            }

            if (committed)
            {
                Utility.drawTextWithShadow(
                    b, I18n.ExitChoice_Waiting(), Game1.smallFont,
                    new Vector2(xPositionOnScreen + 32, yPositionOnScreen + height - 56 + rise),
                    Game1.textColor * alpha);
            }

            if (upperRightCloseButton is not null && !committed)
            {
                int resting = upperRightCloseButton.bounds.Y;
                upperRightCloseButton.bounds.Y += rise;
                upperRightCloseButton.draw(b, tint, 1f);
                upperRightCloseButton.bounds.Y = resting;
            }

            drawMouse(b);
        }
    }
}

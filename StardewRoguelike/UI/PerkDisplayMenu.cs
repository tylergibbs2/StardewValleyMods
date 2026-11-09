using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewRoguelike.Extensions;
using StardewValley;
using StardewValley.Menus;
using System;

namespace StardewRoguelike.UI
{
    internal class PerkDisplayMenu : IClickableMenu
    {
        private int currentDrawHeight;

        private int scrollOffset = 0;

        private int titleDrawHeight;

        private int highestDrawHeight;

        private int perksOnScreen;

        private ClickableTextureComponent upArrow;

        private ClickableTextureComponent downArrow;

        private static readonly Rectangle horizontalLineRectangle = new(0, 256, 60, 60);

        /// <summary>The height of one perk's row, which is also one step of scroll.</summary>
        private const int RowHeight = 100;

        /// <summary>
        /// The gap between the rule under the title and the first row.
        ///
        /// A row's name is drawn eight pixels above the row's own line, so without
        /// this the first one is written over the rule.
        /// </summary>
        private const int RowInset = 24;

        /// <summary>As tall as the list is allowed to get before it has to scroll.</summary>
        private const int MaxHeight = 808;

        public PerkDisplayMenu() : base(0, 0, 0, 0)
        {
            CalculatePosition();
        }

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            CalculatePosition();
        }

        public override void receiveKeyPress(Keys key)
        {
            if ((key == Keys.Escape || Game1.options.doesInputListContain(Game1.options.journalButton, key)) && readyToClose())
            {
                Game1.exitActiveMenu();
                Game1.playSound("bigDeSelect");
            }
        }

        private bool CanScrollUp()
        {
            return PerksHiddenAboveScroll() > 0;
        }

        private bool CanScrollDown()
        {
            return PerksHiddenAboveScroll() + perksOnScreen < Perks.GetActivePerks().Count;
        }

        private int PerksHiddenAboveScroll()
        {
            return Math.Abs(scrollOffset) / RowHeight;
        }

        public override void receiveScrollWheelAction(int direction)
        {
            if (direction > 0 && CanScrollUp())
            {
                scrollOffset += RowHeight;
                Game1.playSound("shiny4");
            }
            else if (direction < 0 && CanScrollDown())
            {
                scrollOffset -= RowHeight;
                Game1.playSound("shiny4");
            }
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);
            if (upArrow.containsPoint(x, y) && CanScrollUp())
            {
                scrollOffset += RowHeight;
                Game1.playSound("shiny4");
                upArrow.scale = upArrow.baseScale;
            }
            else if (downArrow.containsPoint(x, y) && CanScrollDown())
            {
                scrollOffset -= RowHeight;
                Game1.playSound("shiny4");
                downArrow.scale = downArrow.baseScale;
            }
        }

        public override void performHoverAction(int x, int y)
        {
            base.performHoverAction(x, y);
            upArrow.tryHover(x, y);
            downArrow.tryHover(x, y);
        }

        private void CalculatePosition()
        {
            width = (int)(Game1.uiViewport.Width * 0.66f);

            xPositionOnScreen = Game1.uiViewport.Width / 2 - width / 2;
            yPositionOnScreen = 100.ToUIScale();

            int titleHeight = TitleHeight();
            int padding = spaceToClearSideBorder * 2;

            // Sized to what it holds, rather than a fixed 808 that left most of the
            // box empty for a player carrying one or two perks.
            int room = Math.Min(MaxHeight, Game1.uiViewport.Height - yPositionOnScreen)
                       - titleHeight - RowInset - padding;
            int rows = Math.Max(1, Math.Min(Perks.GetActivePerks().Count, room / RowHeight));

            height = titleHeight + RowInset + rows * RowHeight + padding;

            upArrow = new(new Rectangle(xPositionOnScreen + width - 64, yPositionOnScreen + titleHeight + 8, 44, 48), Game1.mouseCursors, new Rectangle(421, 459, 11, 12), 4f);
            downArrow = new(new Rectangle(xPositionOnScreen + width - 64, yPositionOnScreen + height - 64, 44, 48), Game1.mouseCursors, new Rectangle(421, 472, 11, 12), 4f);
        }

        /// <summary>How much of the menu the title and its rule take up.</summary>
        private static int TitleHeight()
        {
            return (int)Game1.dialogueFont.MeasureString(I18n.UI_PerkDisplayMenu_Title()).Y
                   + 32 + spaceToClearSideBorder * 2;
        }

        public void DrawTitle(SpriteBatch b)
        {
            Vector2 textSize = Game1.dialogueFont.MeasureString(I18n.UI_PerkDisplayMenu_Title());

            Utility.drawTextWithShadow(
                b,
                I18n.UI_PerkDisplayMenu_Title(),
                Game1.dialogueFont,
                new Vector2(
                    xPositionOnScreen + (width / 2) - (textSize.X / 2),
                    currentDrawHeight + spaceToClearSideBorder * 2
                ),
                Color.Black
            );

            currentDrawHeight += (int)textSize.Y + 32 + spaceToClearSideBorder * 2;
            titleDrawHeight = currentDrawHeight;

            // Drawn where the list starts, not where the title does. The rule is
            // drawn upward from the line it is given, so asking for it at the top of
            // the menu put it against the top border instead of under the title.
            DrawHorizontalLine(b, titleDrawHeight);

            currentDrawHeight += RowInset;
        }

        public void DrawNoPerksText(SpriteBatch b)
        {
            string noPerks = I18n.UI_PerkDisplayMenu_NoPerks();
            b.DrawString(Game1.smallFont, noPerks, new Vector2(xPositionOnScreen + width / 2 - Game1.smallFont.MeasureString(noPerks).X / 2f, currentDrawHeight + spaceToClearSideBorder), Game1.textColor);
        }

        /// <summary>Draws a divider at <paramref name="y"/>, a screen coordinate.</summary>
        /// <remarks>
        /// Every caller passes currentDrawHeight, which starts at yPositionOnScreen
        /// and is therefore already absolute. Adding yPositionOnScreen again here put
        /// every divider a menu's-worth of pixels below where it belonged.
        /// </remarks>
        public void DrawHorizontalLine(SpriteBatch b, int y)
        {
            drawTextureBox(
                b,
                Game1.menuTexture,
                horizontalLineRectangle,
                xPositionOnScreen,
                y - spaceToClearSideBorder,
                width,
                spaceToClearSideBorder,
                Color.White,
                drawShadow: false
            );
        }

        /// <summary>Whether a row at this screen coordinate falls inside the list.</summary>
        /// <remarks>
        /// titleDrawHeight and yLevel are absolute, so the bounds have to be too:
        /// height on its own is the menu's size, not the screen position of its
        /// bottom edge. A row is drawn whole or not at all, since a row that only
        /// half fits writes its description over the bottom border.
        /// </remarks>
        private bool ShouldDraw(int yLevel)
        {
            return yLevel >= titleDrawHeight
                   && yLevel + RowHeight <= yPositionOnScreen + height - spaceToClearSideBorder;
        }

        public void DrawScrollArrows(SpriteBatch b)
        {
            if (CanScrollUp())
                upArrow.draw(b);
            if (CanScrollDown())
                downArrow.draw(b);
        }

        public void DrawPerk(SpriteBatch b, Perks.PerkType perkType, int level)
        {
            Perks.PerkRarity rarity = Perks.GetPerkRarity(perkType);
            Color rarityColor = PerkMenu.GetRarityColor(rarity);
            Rectangle iconSourceRect = Perks.GetPerkSourceRect(perkType);

            // A perk taken more than once says so, since the list is the only place
            // the player can see how far they have taken anything.
            string perkName = Perks.GetPerkDisplayName(perkType);
            if (level > 1)
                perkName += " " + Perks.Numeral(level);
            string perkDescription = Perks.GetPerkDescription(perkType, level);
            string rarityText = PerkMenu.GetRarityText(rarity);

            Vector2 nameSize = Game1.dialogueFont.MeasureString(perkName);

            if (ShouldDraw(currentDrawHeight + scrollOffset))
            {
                perksOnScreen++;

                b.Draw(
                    Perks.GetPerkIconTexture(perkType),
                    new Vector2(xPositionOnScreen + spaceToClearSideBorder * 2, (currentDrawHeight + scrollOffset)),
                    iconSourceRect,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    4f,
                    SpriteEffects.None,
                    1f
                );

                b.DrawString(
                    Game1.dialogueFont,
                    perkName,
                    new Vector2(xPositionOnScreen + 75 + spaceToClearSideBorder * 2, (currentDrawHeight + scrollOffset) - 8),
                    Game1.textColor
                );

                b.DrawString(
                    Game1.dialogueFont,
                    rarityText,
                    new Vector2(xPositionOnScreen + 100 + (int)nameSize.X + spaceToClearSideBorder * 2, (currentDrawHeight + scrollOffset) - 8),
                    rarityColor
                );

                b.DrawString(
                    Game1.smallFont,
                    Game1.parseText(perkDescription, Game1.smallFont, width - 48),
                    new Vector2(xPositionOnScreen + 75 + spaceToClearSideBorder * 2, (currentDrawHeight + scrollOffset) + 36),
                    Game1.textColor
                );

                bool isLastOnScreen = !ShouldDraw(currentDrawHeight + scrollOffset + RowHeight);

                // At the foot of the row, so it separates this perk from the next
                // one rather than striking through this one's name.
                if (!isLastOnScreen)
                    DrawHorizontalLine(b, (currentDrawHeight + scrollOffset) + RowHeight);
            }

            currentDrawHeight += RowHeight;

            if (currentDrawHeight > highestDrawHeight)
                highestDrawHeight = currentDrawHeight;
        }

        public override void draw(SpriteBatch b)
        {
            currentDrawHeight = yPositionOnScreen;
            perksOnScreen = 0;

            drawTextureBox(
                b,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                xPositionOnScreen,
                yPositionOnScreen,
                width,
                height,
                Color.White,
                drawShadow: true
            );

            DrawTitle(b);

            if (Perks.GetActivePerks().Count == 0)
                DrawNoPerksText(b);
            else
            {
                foreach ((Perks.PerkType perkType, int level) in Perks.GetActivePerks())
                    DrawPerk(b, perkType, level);

                DrawScrollArrows(b);
            }

            drawMouse(b);
        }
    }


}

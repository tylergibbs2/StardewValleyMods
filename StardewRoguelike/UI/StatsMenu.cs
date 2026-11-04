using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewRoguelike.Extensions;
using StardewValley;
using StardewValley.Menus;

namespace StardewRoguelike.UI
{
    public class StatsMenu : IClickableMenu
    {
        private readonly float percentOfViewportWide = 0.33f;
        private readonly float percentOfViewportTall = 0.66f;

        private readonly int borderSize = 60 / 3;

        private readonly int statLinePadding = 6;

        private readonly Rectangle horizontalLineRectangle = new(0, 256, 60, 60);

        private int currentDrawHeight;

        public StatsMenu()
        {
            upperRightCloseButton = new(new(xPositionOnScreen + width - 36, yPositionOnScreen - 8, 48, 48), Game1.mouseCursors, new Rectangle(337, 494, 12, 12), 4f)
            {
                myID = 102
            };

            CalculatePosition();

            populateClickableComponentList();

            if (Game1.options.gamepadControls)
                snapToDefaultClickableComponent();
        }

        public override void snapToDefaultClickableComponent()
        {
            currentlySnappedComponent = upperRightCloseButton;
            snapCursorToCurrentSnappedComponent();
        }

        public override void receiveKeyPress(Keys key)
        {
            Keys menuKey = Game1.options.getFirstKeyboardKeyFromInputButtonList(Game1.options.menuButton);
            Keys journalKey = Game1.options.getFirstKeyboardKeyFromInputButtonList(Game1.options.journalButton);
            if (key == menuKey || key == journalKey)
                Game1.exitActiveMenu();
        }

        public override void receiveGamePadButton(Buttons b)
        {
            base.receiveGamePadButton(b);

            if (currentlySnappedComponent is null)
                snapToDefaultClickableComponent();

            Keys key = Utility.mapGamePadButtonToKey(b);
            receiveKeyPress(key);
        }

        public static void Show()
        {
            Game1.activeClickableMenu = new StatsMenu();
        }

        private void CalculatePosition()
        {
            width = (int)(Game1.uiViewport.Width * percentOfViewportWide);
            height = (int)(Game1.uiViewport.Height * percentOfViewportTall);

            xPositionOnScreen = Game1.uiViewport.Width / 2 - width / 2;
            yPositionOnScreen = 100.ToUIScale();

            upperRightCloseButton.bounds = new(xPositionOnScreen + width - 36, yPositionOnScreen - 8, 48, 48);
        }

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            CalculatePosition();
        }

        public void DrawHorizontalLine(SpriteBatch spriteBatch, int y)
        {
            drawTextureBox(
                spriteBatch,
                Game1.menuTexture,
                horizontalLineRectangle,
                xPositionOnScreen,
                yPositionOnScreen - borderSize + y,
                width,
                borderSize,
                Color.White,
                drawShadow: false
            );

            currentDrawHeight += borderSize;
        }

        public void DrawTitle(SpriteBatch spriteBatch)
        {
            Vector2 textSize = Game1.dialogueFont.MeasureString(I18n.UI_StatsMenu_Title());

            Utility.drawTextWithShadow(
                spriteBatch,
                I18n.UI_StatsMenu_Title(),
                Game1.dialogueFont,
                new Vector2(
                    xPositionOnScreen + (width / 2) - (textSize.X / 2),
                    currentDrawHeight + borderSize
                ),
                Color.Black
            );

            currentDrawHeight += (int)textSize.Y;
        }

        public void DrawStats(SpriteBatch spriteBatch)
        {
            Vector2 textSize = Vector2.Zero;
            foreach (string line in ModEntry.ActiveStats.GetLines())
            {
                textSize = Game1.smallFont.MeasureString(line);

                string label = line.Split(":")[0];
                string value = line.Split(":")[1];

                Utility.drawTextWithShadow(
                    spriteBatch,
                    label,
                    Game1.smallFont,
                    new Vector2(
                        xPositionOnScreen + borderSize,
                        currentDrawHeight + borderSize + statLinePadding
                    ),
                    Color.Black
                );

                int valueWidth = (int)Game1.smallFont.MeasureString(value).X;

                Utility.drawTextWithShadow(
                    spriteBatch,
                    value,
                    Game1.smallFont,
                    new Vector2(
                        xPositionOnScreen + width - (borderSize + valueWidth),
                        currentDrawHeight + borderSize + statLinePadding
                    ),
                    Color.Black
                );

                currentDrawHeight += (int)textSize.Y + statLinePadding;
            }

            currentDrawHeight -= (int)textSize.Y + statLinePadding;
        }

        public override void draw(SpriteBatch spriteBatch)
        {
            currentDrawHeight = yPositionOnScreen;

            drawTextureBox(
                spriteBatch,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                xPositionOnScreen,
                yPositionOnScreen,
                width,
                height,
                Color.White,
                drawShadow: true
            );

            DrawTitle(spriteBatch);
            DrawStats(spriteBatch);

            height = currentDrawHeight;

            upperRightCloseButton.draw(spriteBatch);

            drawMouse(spriteBatch);
        }
    }
}

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewRoguelike.Mode;
using StardewValley;
using StardewValley.Menus;

namespace StardewRoguelike.UI
{
    /// <summary>
    /// The way into a run from the title screen.
    ///
    /// Drawn over the title menu from SMAPI's events rather than by patching it. The
    /// previous version rebuilt TitleMenu.setUpIcons, which meant a Harmony patch had
    /// to stay applied even in a vanilla session just to offer the button, and the
    /// extra button squeezed the vanilla row until it collided with the about and
    /// language icons in the corner.
    ///
    /// It sits in the empty band above the buttons instead, which is clear of
    /// everything that competes for space on this screen: vanilla's own icons are in
    /// the four corners, SMAPI writes update notices along the top, and mod config
    /// menus put their cog down by the button row.
    /// </summary>
    internal static class TitleRoguelikeEntry
    {
        private const int Width = 340;
        private const int Height = 84;

        /// <summary>Clear of the button row below and the title board above.</summary>
        private const int GapAboveButtons = 108;

        private static float scale = 1f;

        /// <summary>Ticks the title screen has been settled, so this arrives last.</summary>
        private static int settledTicks;

        /// <summary>How far through its entrance the plaque is, 0 to 1.</summary>
        private static float appeared;

        /// <summary>Let the vanilla buttons land before this drops in behind them.</summary>
        private const int DelayTicks = 55;

        private const int RiseTicks = 14;

        private static Rectangle Bounds
        {
            get
            {
                int x = (Game1.uiViewport.Width / 2) - (Width / 2);
                int y = Game1.uiViewport.Height - 174 - 24 - GapAboveButtons;
                return new Rectangle(x, y, Width, Height);
            }
        }

        /// <summary>Whether the title screen is settled enough to take a click.</summary>
        private static bool Ready =>
            !RoguelikeMode.IsActive
            && Game1.activeClickableMenu is TitleMenu title
            && TitleMenu.subMenu is null
            && title.titleInPosition
            && !title.isTransitioningButtons
            && !title.transitioningCharacterCreationMenu;

        public static void RenderedActiveMenu(object? sender, RenderedActiveMenuEventArgs e)
        {
            if (!Ready)
            {
                // Back to the start, so leaving and returning to the title replays it.
                settledTicks = 0;
                appeared = 0f;
                return;
            }

            settledTicks++;
            if (settledTicks < DelayTicks)
                return;

            appeared = System.Math.Min(1f, (settledTicks - DelayTicks) / (float)RiseTicks);

            Rectangle bounds = Bounds;
            bool hovered = bounds.Contains(Game1.getOldMouseX(), Game1.getOldMouseY());
            scale = hovered
                ? System.Math.Min(scale + 0.03f, 1.1f)
                : System.Math.Max(scale - 0.03f, 1f);

            // Ease out, and rise the last few pixels into place.
            float eased = 1f - ((1f - appeared) * (1f - appeared));
            int rise = (int)((1f - eased) * 40f);

            int grow = (int)((scale - 1f) * Width);
            Rectangle drawn = new(
                bounds.X - grow / 2,
                bounds.Y - grow / 8 + rise,
                bounds.Width + grow,
                bounds.Height + grow / 4);

            IClickableMenu.drawTextureBox(
                e.SpriteBatch,
                Game1.mouseCursors,
                new Rectangle(403, 373, 9, 9),
                drawn.X, drawn.Y, drawn.Width, drawn.Height,
                (hovered ? Color.Wheat : Color.White) * eased,
                4f,
                drawShadow: true);

            string label = I18n.UI_TitleEntry_Play();
            Vector2 size = Game1.dialogueFont.MeasureString(label);
            Utility.drawTextWithShadow(
                e.SpriteBatch,
                label,
                Game1.dialogueFont,
                new Vector2(
                    drawn.X + (drawn.Width / 2) - (size.X / 2),
                    drawn.Y + (drawn.Height / 2) - (size.Y / 2)),
                Game1.textColor * eased);

            // The menu draws the cursor as the last thing it does, and this event
            // runs after that, so without putting it back the plaque covers it.
            Game1.activeClickableMenu?.drawMouse(e.SpriteBatch);
        }

        public static void ButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Ready)
                return;

            if (appeared < 1f)
                return;

            bool clicked = e.Button == SButton.MouseLeft || e.Button == SButton.ControllerA;
            if (!clicked || !Bounds.Contains(Game1.getOldMouseX(), Game1.getOldMouseY()))
                return;

            // Otherwise the same click also lands on whatever is underneath.
            ModEntry.Instance.Helper.Input.Suppress(e.Button);

            Game1.playSound("bigSelect");
            RoguelikeEntry.Begin();
        }
    }
}

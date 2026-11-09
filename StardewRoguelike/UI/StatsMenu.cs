using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewRoguelike.Extensions;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;

namespace StardewRoguelike.UI
{
    /// <summary>
    /// What a run came to: how long, how far, what it killed and what it cost.
    ///
    /// Drawn as a grouped table rather than a list of lines. It was ten
    /// "label ... value" rows in one black weight, which is the same information
    /// and reads as a config file - nothing told you that four of those numbers
    /// are about fighting and two are about money, and the stat you actually
    /// wanted was found by reading all ten.
    /// </summary>
    public class StatsMenu : IClickableMenu
    {
        /// <summary>The gap between the panel's edge and anything inside it.</summary>
        private const int Padding = 24;

        /// <summary>The square every row's icon is fitted into and centred in.</summary>
        private const int IconBox = 32;

        private const int IconGap = 12;

        /// <summary>How tall one stat is. Comfortably more than the font needs.</summary>
        private const int RowHeight = 40;

        /// <summary>The thickness of a drawn divider, and the gap around one.</summary>
        private const int DividerHeight = 20;

        /// <summary>Breathing room above a section's heading.</summary>
        private const int SectionGap = 10;

        /// <summary>The widest a value is allowed to need, so the panel cannot jitter.</summary>
        /// <remarks>
        /// Measured from the longest reading each kind can produce rather than from
        /// this frame's values. The duration counts up while the menu is open, so a
        /// panel sized to its contents would grow a few pixels mid-run and shunt
        /// every value sideways with it.
        /// </remarks>
        private const string WidestValue = "00h 00m 00s";

        private readonly Rectangle dividerSource = new(0, 256, 60, 60);

        /// <summary>The pill drawn behind a badge. The game's own small button box.</summary>
        private readonly Rectangle badgeSource = new(403, 373, 9, 9);

        /// <summary>Where each stat's icon comes from, and what it looks like.</summary>
        /// <remarks>
        /// Every rectangle here is one the game itself draws, found by reading where
        /// vanilla uses it: the clock is the timed-quest icon from the quest log and
        /// the orders board, the sword, shield and starburst are the damage, defence
        /// and critical-power icons from a weapon's tooltip, the coin is the one the
        /// load menu puts beside a save's money, the heart is a friendship heart and
        /// the arrow is the one this mod's own perk list scrolls with. Nothing here
        /// is a guessed offset into a sprite sheet.
        /// </remarks>
        private static (Texture2D Texture, Rectangle Source) IconFor(Stats.StatId id)
        {
            return id switch
            {
                Stats.StatId.Duration or Stats.StatId.CompletionTime
                    => (Game1.mouseCursors, new Rectangle(410, 501, 9, 9)),

                Stats.StatId.FloorsDescended
                    => (Game1.mouseCursors, new Rectangle(421, 472, 11, 12)),

                Stats.StatId.MonstersKilled
                    => (Game1.objectSpriteSheet,
                        Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, 820, 16, 16)),

                Stats.StatId.BossesDefeated
                    => (Game1.mouseCursors, new Rectangle(160, 428, 10, 10)),

                Stats.StatId.DamageDealt
                    => (Game1.mouseCursors, new Rectangle(120, 428, 10, 10)),

                Stats.StatId.DamageTaken
                    => (Game1.mouseCursors, new Rectangle(110, 428, 10, 10)),

                Stats.StatId.TotalHealing
                    => (Game1.mouseCursors, new Rectangle(211, 428, 4, 6)),

                _ => (Game1.mouseCursors, new Rectangle(193, 373, 9, 9))
            };
        }

        /// <summary>
        /// What colour a value is drawn in, so the three that mean something other
        /// than "more is better" are not the same ink as the rest.
        /// </summary>
        private static Color ColourFor(Stats.StatId id)
        {
            return id switch
            {
                Stats.StatId.GoldEarned or Stats.StatId.GoldSpent => new Color(150, 92, 16),
                Stats.StatId.DamageTaken => new Color(158, 42, 32),
                Stats.StatId.TotalHealing => new Color(32, 112, 52),
                _ => Game1.textColor
            };
        }

        private static readonly Color HeadingColour = new(122, 74, 42);

        private static readonly Color FooterColour = new(126, 104, 92);

        /// <summary>The labels, measured once. Only the values change while open.</summary>
        private int labelColumnWidth;

        private int valueColumnWidth;

        public StatsMenu()
        {
            upperRightCloseButton = new(
                new Rectangle(0, 0, 48, 48), Game1.mouseCursors, new Rectangle(337, 494, 12, 12), 4f)
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

        /// <summary>
        /// Sizes the panel to what it is about to hold, then centres it.
        /// </summary>
        /// <remarks>
        /// Both dimensions come from the content. The width used to be a third of
        /// the viewport whatever was in it, so a narrow window ran the labels into
        /// the values; the height was a fixed two thirds and then overwritten
        /// during the draw with an accumulator that still had the panel's own screen
        /// position in it - a height of "y plus the content", which drew a box
        /// longer than its contents by however far down the screen it had started.
        /// </remarks>
        private void CalculatePosition()
        {
            List<Stats.StatSection> sections = ModEntry.ActiveStats.GetSections();

            labelColumnWidth = 0;
            valueColumnWidth = (int)Game1.smallFont.MeasureString(WidestValue).X;

            int innerHeight = 0;

            foreach (Stats.StatSection section in sections)
            {
                innerHeight += SectionGap + (int)Game1.smallFont.MeasureString(section.Title).Y;
                innerHeight += section.Rows.Count * RowHeight;

                labelColumnWidth = Math.Max(
                    labelColumnWidth, (int)Game1.smallFont.MeasureString(section.Title).X);

                foreach (Stats.StatRow row in section.Rows)
                {
                    labelColumnWidth = Math.Max(
                        labelColumnWidth,
                        IconBox + IconGap + (int)Game1.smallFont.MeasureString(row.Label).X);

                    valueColumnWidth = Math.Max(
                        valueColumnWidth, (int)Game1.smallFont.MeasureString(row.Value).X);
                }
            }

            int titleHeight = (int)Game1.dialogueFont.MeasureString(I18n.UI_StatsMenu_Title()).Y;
            int footerHeight = (int)Game1.smallFont.MeasureString(Footer()).Y;

            width = Math.Max(
                labelColumnWidth + IconGap * 3 + valueColumnWidth + Padding * 2,
                (int)Game1.dialogueFont.MeasureString(I18n.UI_StatsMenu_Title()).X + Padding * 4);

            height =
                Padding
                + titleHeight
                + DividerHeight
                + (Badges().Count > 0 ? RowHeight : 0)
                + innerHeight
                + DividerHeight
                + footerHeight
                + Padding;

            xPositionOnScreen = Game1.uiViewport.Width / 2 - width / 2;
            yPositionOnScreen = Math.Max(
                16, Game1.uiViewport.Height / 2 - height / 2 - 24.ToUIScale());

            upperRightCloseButton.bounds = new(
                xPositionOnScreen + width - 36, yPositionOnScreen - 8, 48, 48);
        }

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            CalculatePosition();
        }

        /// <summary>The things worth saying about a run that are not numbers.</summary>
        private static List<string> Badges()
        {
            List<string> badges = new();

            if (Roguelike.HardMode)
                badges.Add(I18n.UI_StatsMenu_HardMode());

            int players = Game1.getOnlineFarmers().Count;
            if (players > 1)
                badges.Add(I18n.UI_StatsMenu_Players(count: players));

            return badges;
        }

        /// <summary>
        /// The seed and the version, which were kept for every run and never shown.
        /// </summary>
        /// <remarks>
        /// Read live rather than from the record, because the record's copies are
        /// filled in as the run ends - so until then they would both read as blank
        /// on the one screen a player would go looking for a seed on.
        /// </remarks>
        private static string Footer()
        {
            return I18n.UI_StatsMenu_Footer(
                seed: Roguelike.FloorRngSeed.ToString(),
                patch: ModEntry.CurrentVersion ?? "?");
        }

        private void DrawDivider(SpriteBatch b, int y)
        {
            drawTextureBox(
                b,
                Game1.menuTexture,
                dividerSource,
                xPositionOnScreen + Padding / 2,
                y,
                width - Padding,
                DividerHeight,
                Color.White,
                drawShadow: false);
        }

        /// <summary>Draws one icon fitted to <see cref="IconBox"/> and centred in it.</summary>
        /// <remarks>
        /// The icons are not one size - a friendship heart is four pixels by six and
        /// an object sprite is sixteen square - so each is scaled to the box rather
        /// than drawn at a fixed multiple, and capped so the smallest of them is not
        /// blown up to five times its size to fill the space.
        /// </remarks>
        private static void DrawIcon(SpriteBatch b, Stats.StatId id, int x, int y)
        {
            (Texture2D texture, Rectangle source) = IconFor(id);

            float scale = Math.Min(4f, (float)IconBox / Math.Max(source.Width, source.Height));

            Utility.drawWithShadow(
                b,
                texture,
                new Vector2(
                    x + (IconBox - source.Width * scale) / 2f,
                    y + (IconBox - source.Height * scale) / 2f),
                source,
                Color.White,
                0f,
                Vector2.Zero,
                scale,
                flipped: false,
                1f);
        }

        private int DrawBadges(SpriteBatch b, int y)
        {
            List<string> badges = Badges();
            if (badges.Count == 0)
                return y;

            int totalWidth = 0;
            foreach (string badge in badges)
                totalWidth += (int)Game1.smallFont.MeasureString(badge).X + Padding * 2 + IconGap;

            int x = xPositionOnScreen + width / 2 - (totalWidth - IconGap) / 2;

            foreach (string badge in badges)
            {
                Vector2 size = Game1.smallFont.MeasureString(badge);
                int pillWidth = (int)size.X + Padding * 2;

                drawTextureBox(
                    b, Game1.mouseCursors, badgeSource,
                    x, y, pillWidth, RowHeight - 6, Color.White, drawShadow: false);

                Utility.drawTextWithShadow(
                    b, badge, Game1.smallFont,
                    new Vector2(x + Padding, y + (RowHeight - 6 - size.Y) / 2f),
                    HeadingColour);

                x += pillWidth + IconGap;
            }

            return y + RowHeight;
        }

        public override void draw(SpriteBatch b)
        {
            drawTextureBox(
                b,
                Game1.menuTexture,
                dividerSource,
                xPositionOnScreen,
                yPositionOnScreen,
                width,
                height,
                Color.White,
                drawShadow: true);

            int y = yPositionOnScreen + Padding;

            Vector2 titleSize = Game1.dialogueFont.MeasureString(I18n.UI_StatsMenu_Title());
            Utility.drawTextWithShadow(
                b,
                I18n.UI_StatsMenu_Title(),
                Game1.dialogueFont,
                new Vector2(xPositionOnScreen + width / 2 - titleSize.X / 2, y),
                Game1.textColor);

            y += (int)titleSize.Y;

            DrawDivider(b, y);
            y += DividerHeight;

            y = DrawBadges(b, y);

            int row = 0;
            foreach (Stats.StatSection section in ModEntry.ActiveStats.GetSections())
            {
                y += SectionGap;

                Utility.drawTextWithShadow(
                    b, section.Title, Game1.smallFont,
                    new Vector2(xPositionOnScreen + Padding, y), HeadingColour);

                y += (int)Game1.smallFont.MeasureString(section.Title).Y;

                foreach (Stats.StatRow stat in section.Rows)
                {
                    // Every other row carries the faintest wash behind it. Ten
                    // numbers in two columns is exactly the shape that has a reader
                    // checking twice which value belongs to which label.
                    if (row++ % 2 == 1)
                    {
                        b.Draw(
                            Game1.staminaRect,
                            new Rectangle(
                                xPositionOnScreen + Padding / 2, y, width - Padding, RowHeight),
                            Color.Black * 0.05f);
                    }

                    DrawIcon(b, stat.Id, xPositionOnScreen + Padding, y + (RowHeight - IconBox) / 2);

                    Vector2 labelSize = Game1.smallFont.MeasureString(stat.Label);
                    Utility.drawTextWithShadow(
                        b, stat.Label, Game1.smallFont,
                        new Vector2(
                            xPositionOnScreen + Padding + IconBox + IconGap,
                            y + (RowHeight - labelSize.Y) / 2f),
                        Game1.textColor);

                    Vector2 valueSize = Game1.smallFont.MeasureString(stat.Value);
                    Utility.drawTextWithShadow(
                        b, stat.Value, Game1.smallFont,
                        new Vector2(
                            xPositionOnScreen + width - Padding - valueSize.X,
                            y + (RowHeight - valueSize.Y) / 2f),
                        ColourFor(stat.Id));

                    y += RowHeight;
                }
            }

            DrawDivider(b, y);
            y += DividerHeight;

            string footer = Footer();
            Vector2 footerSize = Game1.smallFont.MeasureString(footer);
            Utility.drawTextWithShadow(
                b, footer, Game1.smallFont,
                new Vector2(xPositionOnScreen + width / 2 - footerSize.X / 2, y), FooterColour);

            upperRightCloseButton.draw(b);

            drawMouse(b);
        }
    }
}

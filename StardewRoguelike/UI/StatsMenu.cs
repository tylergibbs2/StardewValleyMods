using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;

namespace StardewRoguelike.UI
{
    /// <summary>
    /// What a run came to: how long, how far, what it killed and what it cost.
    ///
    /// Drawn as grouped columns rather than a list of lines. It was ten
    /// "label ... value" rows in one black weight, which is the same information
    /// and reads as a config file - nothing told you that five of those numbers
    /// are about fighting and two are about money, and the stat you actually
    /// wanted was found by reading all ten.
    /// </summary>
    public class StatsMenu : IClickableMenu
    {
        /// <summary>The gap between the panel's edge and anything inside it.</summary>
        private const int Padding = 28;

        /// <summary>The square every row's icon is fitted into and centred in.</summary>
        private const int IconBox = 32;

        private const int IconGap = 12;

        /// <summary>How tall one stat is.</summary>
        private const int RowHeight = 46;

        /// <summary>The thickness of a drawn divider, and the gap around one.</summary>
        private const int DividerHeight = 20;

        /// <summary>Breathing room above a section's heading, and below it.</summary>
        private const int SectionGap = 18;

        private const int HeaderGap = 8;

        /// <summary>The space between the two columns of sections.</summary>
        private const int Gutter = 44;

        /// <summary>The band the seed and version sit in, under the last rule.</summary>
        private const int FooterBand = 56;

        /// <summary>The panel frame's own thickness, from its sixty-pixel corner art.</summary>
        private const int FrameThickness = 20;

        /// <summary>How much of the window the panel may take up before it folds.</summary>
        private const float MaxWidthShare = 0.92f;

        /// <summary>The widest a value is allowed to need, so the panel cannot jitter.</summary>
        /// <remarks>
        /// Measured from the longest reading a stat can produce rather than from this
        /// frame's values. The duration counts up while the menu is open, so a panel
        /// sized to its contents would grow a few pixels mid-run and shunt every
        /// value sideways with it.
        /// </remarks>
        private const string WidestValue = "00h 00m 00s";

        private readonly Rectangle dividerSource = new(0, 256, 60, 60);

        /// <summary>The pill drawn behind a badge. The game's own small button box.</summary>
        private readonly Rectangle badgeSource = new(403, 373, 9, 9);

        /// <summary>One side of the panel: the sections dealt to it, and its measurements.</summary>
        private sealed class Column
        {
            public readonly List<Stats.StatSection> Sections = new();

            public int LabelWidth;

            public int ValueWidth;

            public int Height;

            /// <summary>Where it starts, once the panel knows where it is.</summary>
            public int X;

            /// <summary>Label column, three gaps of air, then the values.</summary>
            public int Width => LabelWidth + IconGap * 3 + ValueWidth;
        }

        private List<Column> columns = new();

        /// <summary>Where each stat's icon comes from, and what it looks like.</summary>
        /// <remarks>
        /// Every rectangle here is one the game itself draws, found by reading where
        /// vanilla uses it: the clock is the timed-quest icon from the quest log and
        /// the orders board, the sword and shield are the damage and defence icons
        /// from a weapon's tooltip, the coin is the one the load menu puts beside a
        /// save's money, the heart is a friendship heart and the arrow is the one
        /// this mod's own perk list scrolls with. Nothing here is a guessed offset
        /// into a sprite sheet.
        /// </remarks>
        private static (Texture2D Texture, Rectangle Source) IconFor(Stats.StatId id)
        {
            return id switch
            {
                // The clock for time passing, the tick for time stopped. The pair
                // shared a clock, which is the one thing they have in common and
                // the one thing a reader does not need telling.
                Stats.StatId.Duration
                    => (Game1.mouseCursors, new Rectangle(410, 501, 9, 9)),

                Stats.StatId.CompletionTime
                    => (Game1.mouseCursors, new Rectangle(236, 425, 9, 9)),

                Stats.StatId.FloorsDescended
                    => (Game1.mouseCursors, new Rectangle(421, 472, 11, 12)),

                // A skull for what died and a dragon's tooth for what it cost to
                // kill the big ones. The critical-power icon stood in for bosses and
                // drew as a small green bottle, which reads as a potion.
                Stats.StatId.MonstersKilled => Obj(820),
                Stats.StatId.BossesDefeated => Obj(852),

                Stats.StatId.DamageDealt
                    => (Game1.mouseCursors, new Rectangle(120, 428, 10, 10)),

                Stats.StatId.DamageTaken
                    => (Game1.mouseCursors, new Rectangle(110, 428, 10, 10)),

                Stats.StatId.TotalHealing
                    => (Game1.mouseCursors, new Rectangle(211, 428, 4, 6)),

                // A bar for what was kept and a coin for what left the pocket.
                // Both unmistakably money, and not the same sprite twice.
                Stats.StatId.GoldEarned => Obj(336),

                _ => (Game1.mouseCursors, new Rectangle(193, 373, 9, 9))
            };
        }

        private static (Texture2D, Rectangle) Obj(int index)
        {
            return (
                Game1.objectSpriteSheet,
                Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, index, 16, 16));
        }

        /// <summary>
        /// What colour a value is drawn in, so the three that mean something other
        /// than "more is better" are not the same ink as the rest.
        /// </summary>
        private static Color ColourFor(Stats.StatId id)
        {
            return id switch
            {
                Stats.StatId.GoldEarned or Stats.StatId.GoldSpent => new Color(140, 82, 10),
                Stats.StatId.DamageTaken => new Color(158, 42, 32),
                Stats.StatId.TotalHealing => new Color(32, 112, 52),
                _ => Game1.textColor
            };
        }

        /// <summary>
        /// The characters a line is centred against, so every row shares a baseline.
        /// </summary>
        /// <remarks>
        /// A cap and a digit. Centring each string against its own ink would put a
        /// label with a descender in it half a pixel off its own value, which is
        /// exactly the wobble this is meant to remove.
        /// </remarks>
        private const string InkReference = "0A";

        /// <summary>
        /// Where a line of <paramref name="font"/> sits to look centred in a band.
        /// </summary>
        /// <remarks>
        /// Measured off the ink rather than the line box. MeasureString reports the
        /// whole box, including the descender space under a line that has no
        /// descenders in it, and that space is all at the bottom - so centring the
        /// box leaves the text sitting high by half the descent. On a row it is
        /// barely visible; on the footer, alone under a rule, it was plain.
        ///
        /// Falls back to the line box for a font whose glyphs cannot be read, which
        /// is no worse than what this replaces.
        /// </remarks>
        private static float CentredTextY(SpriteFont font, float bandTop, float bandHeight)
        {
            try
            {
                float top = float.MaxValue;
                float bottom = float.MinValue;

                foreach (char c in InkReference)
                {
                    if (!font.GetGlyphs().TryGetValue(c, out SpriteFont.Glyph glyph))
                        continue;

                    top = Math.Min(top, glyph.Cropping.Y);
                    bottom = Math.Max(bottom, glyph.Cropping.Y + glyph.BoundsInTexture.Height);
                }

                if (top <= bottom)
                    return bandTop + (bandHeight - (bottom - top)) / 2f - top;
            }
            catch
            {
                // A font that will not describe its glyphs gets the old centring.
            }

            return bandTop + (bandHeight - font.MeasureString(InkReference).Y) / 2f;
        }

        /// <summary>
        /// Swaps out any character <paramref name="font"/> has no glyph for.
        /// </summary>
        /// <remarks>
        /// The em dash is not in every one of the game's fonts, and drawing a
        /// character a SpriteFont does not carry throws rather than drawing a blank.
        /// One missing glyph on the stats screen is not worth a crash, so the dash
        /// degrades to a hyphen wherever it cannot be drawn.
        /// </remarks>
        private static string Drawable(SpriteFont font, string text)
        {
            try
            {
                if (font.DefaultCharacter is not null)
                    return text;

                System.Collections.Generic.Dictionary<char, SpriteFont.Glyph> glyphs = font.GetGlyphs();

                foreach (char c in text)
                {
                    if (glyphs.ContainsKey(c))
                        continue;

                    return text.Replace(Stats.NoReading, "-");
                }
            }
            catch
            {
                return text.Replace(Stats.NoReading, "-");
            }

            return text;
        }

        private static readonly Color HeadingColour = new(122, 74, 42);

        private static readonly Color FooterColour = new(126, 104, 92);

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

        /// <summary>How tall a section is once drawn.</summary>
        private static int HeightOf(Stats.StatSection section)
        {
            return SectionGap
                + (int)Game1.smallFont.MeasureString(section.Title).Y
                + HeaderGap
                + section.Rows.Count * RowHeight;
        }

        /// <summary>
        /// Deals the sections across <paramref name="count"/> columns and measures each.
        /// </summary>
        /// <remarks>
        /// Each section goes to whichever column is currently shortest, rather than
        /// being cut at the half-way mark. Filling one column until it is half the
        /// total puts the section that crosses the line wholly on the left, which
        /// with three sections of three, five and two rows leaves eight against two.
        /// Shortest-first gives five against five.
        /// </remarks>
        private static List<Column> Pack(List<Stats.StatSection> sections, int count)
        {
            List<Column> packed = new();
            for (int i = 0; i < count; i++)
                packed.Add(new Column());

            foreach (Stats.StatSection section in sections)
            {
                Column into = packed[0];
                foreach (Column column in packed)
                {
                    if (column.Height < into.Height)
                        into = column;
                }

                into.Sections.Add(section);
                into.Height += HeightOf(section);
            }

            foreach (Column column in packed)
            {
                column.ValueWidth = (int)Game1.smallFont.MeasureString(WidestValue).X;

                foreach (Stats.StatSection section in column.Sections)
                {
                    column.LabelWidth = Math.Max(
                        column.LabelWidth, (int)Game1.smallFont.MeasureString(section.Title).X);

                    foreach (Stats.StatRow row in section.Rows)
                    {
                        column.LabelWidth = Math.Max(
                            column.LabelWidth,
                            IconBox + IconGap + (int)Game1.smallFont.MeasureString(row.Label).X);

                        column.ValueWidth = Math.Max(
                            column.ValueWidth, (int)Game1.smallFont.MeasureString(row.Value).X);
                    }
                }
            }

            // A column that was dealt nothing would otherwise be charged for its
            // padding and leave a gutter against the panel's edge.
            packed.RemoveAll(column => column.Sections.Count == 0);

            return packed;
        }

        private static int WidthOf(List<Column> packed)
        {
            int total = Padding * 2 + Gutter * (packed.Count - 1);
            foreach (Column column in packed)
                total += column.Width;

            return total;
        }

        /// <summary>
        /// Sizes the panel to what it is about to hold, then centres it.
        /// </summary>
        /// <remarks>
        /// Two columns where they fit and one where they do not, so a narrow window
        /// gets a tall panel rather than one running off both sides of the screen.
        ///
        /// Both dimensions come from the content. The width used to be a third of
        /// the viewport whatever was in it, so a narrow window ran the labels into
        /// the values; the height was a fixed two thirds and then overwritten during
        /// the draw with an accumulator that still had the panel's own screen
        /// position in it - a height of "y plus the content", which drew a box
        /// longer than its contents by however far down the screen it had started.
        /// </remarks>
        private void CalculatePosition()
        {
            List<Stats.StatSection> sections = ModEntry.ActiveStats.GetSections();

            columns = Pack(sections, 2);

            if (WidthOf(columns) > Game1.uiViewport.Width * MaxWidthShare)
                columns = Pack(sections, 1);

            int tallest = 0;
            foreach (Column column in columns)
                tallest = Math.Max(tallest, column.Height);

            Vector2 titleSize = Game1.dialogueFont.MeasureString(I18n.UI_StatsMenu_Title());

            width = Math.Max(WidthOf(columns), (int)titleSize.X + Padding * 4);

            // The footer gets a band of its own rather than its measured height plus
            // whatever padding was left over. The text was drawn at the top of that
            // leftover, so every pixel of slack sat under it.
            height =
                Padding
                + (int)titleSize.Y
                + DividerHeight
                + (Badges().Count > 0 ? RowHeight : 0)
                + tallest
                + SectionGap
                + DividerHeight
                + FooterBand
                + FrameThickness;

            xPositionOnScreen = Game1.uiViewport.Width / 2 - width / 2;
            yPositionOnScreen = Math.Max(16, Game1.uiViewport.Height / 2 - height / 2);

            int x = xPositionOnScreen + Padding;
            foreach (Column column in columns)
            {
                column.X = x;
                x += column.Width + Gutter;
            }

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

        /// <summary>
        /// Draws a rule across the panel, edge to edge.
        /// </summary>
        /// <remarks>
        /// The full width, not inset by the padding. A divider is a box in its own
        /// right and carries its own end caps, so one drawn narrower than the panel
        /// finishes in mid air a few pixels short of the frame instead of meeting
        /// it.
        /// </remarks>
        private void DrawDivider(SpriteBatch b, int y)
        {
            drawTextureBox(
                b,
                Game1.menuTexture,
                dividerSource,
                xPositionOnScreen,
                y,
                width,
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
                    x, y, pillWidth, RowHeight - 8, Color.White, drawShadow: false);

                Utility.drawTextWithShadow(
                    b, badge, Game1.smallFont,
                    new Vector2(x + Padding, CentredTextY(Game1.smallFont, y, RowHeight - 8)),
                    HeadingColour);

                x += pillWidth + IconGap;
            }

            return y + RowHeight;
        }

        private void DrawColumn(SpriteBatch b, Column column, int top)
        {
            int y = top;

            foreach (Stats.StatSection section in column.Sections)
            {
                y += SectionGap;

                Utility.drawTextWithShadow(
                    b, section.Title, Game1.smallFont,
                    new Vector2(column.X, y), HeadingColour);

                y += (int)Game1.smallFont.MeasureString(section.Title).Y + HeaderGap;

                // Counted from the top of each section rather than the top of the
                // column. Carried through, the banding drifted: the left column's
                // second heading pushes its rows half a step out against the right
                // column's, so two stripes that start the panel level come apart
                // lower down and read as a mistake rather than a rhythm. Restarted
                // under every heading, the rule is one a reader can see - the second
                // row of a group is shaded, in both columns, always.
                int row = 0;

                foreach (Stats.StatRow stat in section.Rows)
                {
                    if (row++ % 2 == 1)
                    {
                        b.Draw(
                            Game1.staminaRect,
                            new Rectangle(
                                column.X - IconGap / 2, y, column.Width + IconGap, RowHeight),
                            Color.Black * 0.05f);
                    }

                    DrawIcon(b, stat.Id, column.X, y + (RowHeight - IconBox) / 2);

                    float textY = CentredTextY(Game1.smallFont, y, RowHeight);

                    Utility.drawTextWithShadow(
                        b, stat.Label, Game1.smallFont,
                        new Vector2(column.X + IconBox + IconGap, textY),
                        Game1.textColor);

                    string value = Drawable(Game1.smallFont, stat.Value);
                    float valueWidth = Game1.smallFont.MeasureString(value).X;

                    Utility.drawTextWithShadow(
                        b, value, Game1.smallFont,
                        new Vector2(column.X + column.Width - valueWidth, textY),
                        ColourFor(stat.Id));

                    y += RowHeight;
                }
            }
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

            // Both columns start level, whatever each one holds.
            int tallest = 0;
            foreach (Column column in columns)
            {
                DrawColumn(b, column, y);
                tallest = Math.Max(tallest, column.Height);
            }

            y += tallest + SectionGap;

            DrawDivider(b, y);
            y += DividerHeight;

            string footer = Footer();
            Vector2 footerSize = Game1.smallFont.MeasureString(footer);
            Utility.drawTextWithShadow(
                b, footer, Game1.smallFont,
                new Vector2(
                    xPositionOnScreen + width / 2 - footerSize.X / 2,
                    CentredTextY(Game1.smallFont, y, FooterBand)),
                FooterColour);

            upperRightCloseButton.draw(b);

            drawMouse(b);
        }
    }
}

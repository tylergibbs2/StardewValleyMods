using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewRoguelike.Bosses;
using StardewRoguelike.Run;
using StardewValley;
using StardewValley.Menus;
using System;
using System.Collections.Generic;

namespace StardewRoguelike.UI
{
    /// <summary>
    /// The run as a road: a floor to a line, descending, with the forks it took.
    ///
    /// Where a floor has been settled the exit that led to it is drawn out in full -
    /// every kind that was on the menu, with the one the group walked into lit and
    /// the others left dark. The road is a record of the choices as well as the
    /// route, and a floor not yet reached is a single closed door.
    /// </summary>
    /// <remarks>
    /// The plan knows the whole run from its seed, so this could name every exit
    /// down to the last. It deliberately does not. A route that can be read in
    /// advance is solved rather than chosen. What is shown is what the player could
    /// already work out - a merchant every sixth floor, a boss before each one -
    /// plus their own history, which is theirs.
    ///
    /// The page draws its own frame and its own close button, which a page usually
    /// does not have to. GameMenu sets invisible for the map tab and then skips its
    /// backdrop, its tab strip and its close button, because vanilla's map is a
    /// picture that frames itself. Taking the tab means taking the job.
    /// </remarks>
    public class RunMapPage : IClickableMenu
    {
        private const int Padding = 32;

        /// <summary>The room the title takes above the road.</summary>
        private const int TitleHeight = 60;

        /// <summary>What a floor the player knows something about is given.</summary>
        private const int KnownRowHeight = 76;

        /// <summary>And what one they know nothing about gets.</summary>
        /// <remarks>
        /// Less than half. A fresh run knows nothing about forty-seven of its
        /// forty-eight floors, and giving each of them a full row made the page a
        /// thin line of studs in a lot of parchment. Paying for what a row actually
        /// holds puts three times as much road on screen and lets the floors that
        /// have something to say have the room.
        /// </remarks>
        private const int UnknownRowHeight = 34;

        private const int NodeSize = 48;

        private const int BossNodeSize = 60;

        /// <summary>How far apart two forks of the same exit stand.</summary>
        private const int SlotWidth = 112;

        /// <summary>The column of floor numbers down the left.</summary>
        private const int NumberGutter = 48;

        /// <summary>The room kept beside the forks for a floor's perk and curse marks.</summary>
        private const int MarkGutter = 108;

        private const int MarkSize = 24;

        /// <summary>
        /// How wide the page lets itself be, whatever room the game menu gave it.
        /// </summary>
        /// <remarks>
        /// The menu hands this page the better part of a thousand pixels and the road
        /// needs about half of them. Filling the rest left the marks for a floor two
        /// hundred pixels from the floor they belonged to, which read as a separate
        /// panel rather than as part of the line. Narrow and centred, the road, its
        /// numbers and its marks are one object.
        /// </remarks>
        private const int MaxPanelWidth = 660;

        /// <summary>The column the scroll arrows get, down the right-hand edge.</summary>
        /// <remarks>
        /// Their own column, because they have to live somewhere. Beside the floors'
        /// marks they sat on top of them; above and below the road they hung off the
        /// title and out through the bottom of the frame. Sixty pixels of margin is
        /// cheaper than either.
        /// </remarks>
        private const int ScrollGutter = 60;

        /// <summary>A floor nobody has reached: a mark on the road, not a door.</summary>
        /// <remarks>
        /// These used to be full nodes with a question mark in them, which meant the
        /// unwalked half of a run was a column of identical boxes - the dullest thing
        /// on the page and most of its area. Small and quiet, they read as road yet
        /// to be walked, and the bosses and merchants that are known punctuate it.
        /// </remarks>
        private const int UnknownNodeSize = 20;

        private const int MarkGap = 4;

        private const int ThreadThickness = 5;

        private static readonly Color MutedColour = new(126, 104, 92);

        private static readonly Color RoadColour = new(104, 68, 38);

        /// <summary>The first floor drawn. Scrolling moves this a whole floor at a time.</summary>
        /// <remarks>
        /// Whole floors rather than pixels, so a row is never half drawn and the page
        /// never needs to clip. No menu in the base game clips a scrolling region, so
        /// there is no pattern here to copy, and a scissor rectangle would have to
        /// get the interface scale right on its own.
        /// </remarks>
        private int firstFloor = 1;

        private int fieldHeight;

        /// <summary>The floors on screen, and where each one's line sits.</summary>
        private readonly List<(int Level, int Top, int Height)> window = new();

        private int roadCentreX;

        private int roadTop;

        private int panelX;

        private int panelWidth;

        private ClickableTextureComponent upArrow = null!;

        private ClickableTextureComponent downArrow = null!;

        private string hover = "";

        private string hoverTitle = "";

        /// <summary>Where each drawn node is, so hovering and drawing agree.</summary>
        private readonly List<(Rectangle Bounds, int Level, FloorKind? Kind, bool Taken)> drawn = new();

        /// <summary>Where each drawn perk or curse mark is.</summary>
        private readonly List<(Rectangle Bounds, string Title, string Text)> marks = new();

        public RunMapPage(int x, int y, int width, int height)
            : base(x, y, width, height)
        {
            upperRightCloseButton = new(
                new Rectangle(0, 0, 48, 48), Game1.mouseCursors, new Rectangle(337, 494, 12, 12), 4f);

            Layout();
            ScrollTo(Roguelike.CurrentLevel);
        }

        /// <summary>How far down the road is drawn.</summary>
        /// <remarks>
        /// The planned end, or a cycle past wherever the group has got to. A run
        /// does not stop at the bottom of the plan - it loops, and the bosses come
        /// round again - so stopping the road at the last floor walked would leave a
        /// looping run with nothing ahead of it at all.
        /// </remarks>
        private static int LastFloor =>
            Math.Max(Constants.ScalingOrder[^1], Roguelike.CurrentLevel + Cycle);

        /// <summary>Floors between one boss and the next.</summary>
        private const int Cycle = 6;

        private void Layout()
        {
            fieldHeight = height - Padding * 2 - TitleHeight;

            panelWidth = Math.Min(width, MaxPanelWidth);
            panelX = xPositionOnScreen + (width - panelWidth) / 2;

            roadTop = yPositionOnScreen + Padding + TitleHeight;
            roadCentreX = panelX + NumberGutter
                + (panelWidth - NumberGutter - MarkGutter - ScrollGutter) / 2;

            upperRightCloseButton.bounds = new(
                panelX + panelWidth - 52, yPositionOnScreen + 12, 48, 48);

            // Above and below the road rather than beside it. Down the right-hand
            // edge they sat on top of the floors' marks, which is the one column
            // this page had just finished moving them into.
            int arrowX = panelX + panelWidth - 56;

            upArrow = new(
                new Rectangle(arrowX, roadTop + 4, 33, 36),
                Game1.mouseCursors, new Rectangle(421, 459, 11, 12), 3f);

            downArrow = new(
                new Rectangle(arrowX, roadTop + fieldHeight - 40, 33, 36),
                Game1.mouseCursors, new Rectangle(421, 472, 11, 12), 3f);
        }

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            base.gameWindowSizeChanged(oldBounds, newBounds);
            Layout();
            ScrollTo(Roguelike.CurrentLevel);
        }

        private static int RowHeightFor(int level) =>
            Known(level) ? KnownRowHeight : UnknownRowHeight;

        /// <summary>Works out which floors fit on screen from <see cref="firstFloor"/>.</summary>
        private void BuildWindow()
        {
            window.Clear();

            int y = roadTop;
            for (int level = firstFloor; level <= LastFloor; level++)
            {
                int rowHeight = RowHeightFor(level);
                if (y + rowHeight > roadTop + fieldHeight)
                    break;

                window.Add((level, y, rowHeight));
                y += rowHeight;
            }
        }

        /// <summary>
        /// The furthest the road can be scrolled before it runs out of floors.
        /// </summary>
        /// <remarks>
        /// Counted backwards from the last floor, because the rows are not all the
        /// same height any more and "how many fit" has no single answer.
        /// </remarks>
        private int MaxFirstFloor()
        {
            int y = 0;
            for (int level = LastFloor; level >= 1; level--)
            {
                y += RowHeightFor(level);
                if (y > fieldHeight)
                    return Math.Min(LastFloor, level + 1);
            }

            return 1;
        }

        /// <summary>Puts a floor near the top of the view, as far as the ends allow.</summary>
        private void ScrollTo(int level) => firstFloor = Clamp(level - 2);

        private int Clamp(int first) => Math.Max(1, Math.Min(first, MaxFirstFloor()));

        /// <summary>Where a visible floor's line begins, or null if it is off screen.</summary>
        private int? TopOf(int level)
        {
            foreach ((int at, int top, int _) in window)
            {
                if (at == level)
                    return top;
            }

            return null;
        }

        public override bool shouldDrawCloseButton() => false;

        public override void snapToDefaultClickableComponent() { }

        public override void receiveScrollWheelAction(int direction)
        {
            firstFloor = Clamp(firstFloor + (direction > 0 ? -1 : 1));
        }

        /// <summary>
        /// Closes on the button that opened it, as well as the menu button.
        /// </summary>
        /// <remarks>
        /// GameMenu closes on the menu button and then hands the key down to whichever
        /// page is open, so a page that wants the map button has to take it itself.
        /// Pressing M to open something and finding M will not shut it reads as the
        /// menu being broken.
        /// </remarks>
        public override void receiveKeyPress(Keys key)
        {
            if (Game1.options.doesInputListContain(Game1.options.mapButton, key) && readyToClose())
            {
                Game1.exitActiveMenu();
                Game1.playSound("bigDeSelect");
                return;
            }

            if (Game1.options.doesInputListContain(Game1.options.moveUpButton, key))
                firstFloor = Clamp(firstFloor - 1);
            else if (Game1.options.doesInputListContain(Game1.options.moveDownButton, key))
                firstFloor = Clamp(firstFloor + 1);
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (upperRightCloseButton.containsPoint(x, y))
            {
                Game1.exitActiveMenu();
                Game1.playSound("bigDeSelect");
                return;
            }

            if (upArrow.containsPoint(x, y))
            {
                firstFloor = Clamp(firstFloor - 1);
                Game1.playSound("shwip");
            }
            else if (downArrow.containsPoint(x, y))
            {
                firstFloor = Clamp(firstFloor + 1);
                Game1.playSound("shwip");
            }
        }

        private static bool Reached(int level) => Roguelike.CurrentLevel >= level;

        /// <summary>Whether a floor's shape is the player's to see.</summary>
        /// <remarks>
        /// Walked, or chosen and not yet walked into. Nothing else.
        ///
        /// Boss and merchant floors used to show themselves the whole way down on
        /// the grounds that a merchant every sixth floor is arithmetic rather than
        /// foresight. True, and it still read as the map keeping two different
        /// secrets: it would not say which three ways an exit five floors down
        /// offered, but it would happily say that the floor after it held a boss.
        /// Either the road ahead is known or it is not, and a player who has played
        /// twice knows where the bosses are without being told.
        ///
        /// Asking WasChosen rather than IsSettled, because a forced floor counts as
        /// settled from the first second of the run - that is what tells the exit
        /// not to offer a menu - so leaning on it put every boss in the run back on
        /// the map the moment the last change claimed to have taken them off.
        /// </remarks>
        private static bool Known(int level) => Reached(level) || RunRoute.WasChosen(level);

        /// <summary>
        /// The forks that exit offered, or nothing where the floor is still closed.
        /// </summary>
        /// <remarks>
        /// Read back off the route, which is how the exit menu worked it out at the
        /// time, so the map shows the menu the player actually saw rather than a
        /// fresh roll. Forced floors answer with the single kind they are.
        /// </remarks>
        private static FloorKind[] Forks(int level) =>
            Known(level) ? RunRoute.OffersFor(level) : Array.Empty<FloorKind>();

        private static FloorKind? Taken(int level) =>
            Known(level) ? RunRoute.KindFor(level) : null;

        /// <summary>
        /// A trophy standing for each boss, so a floor can show what waits on it
        /// without naming it.
        /// </summary>
        /// <remarks>
        /// Objects off the game's own sheet rather than the monsters' sprites. A
        /// boss's sheet has its own frame size, several of these bosses set theirs by
        /// hand at construction, and cropping a portrait out of one means either
        /// building a boss to ask it or keeping a second copy of every frame size
        /// here. A trophy reads at this size and cannot be cropped wrong.
        /// </remarks>
        private static int BossToken(Type? boss) => boss?.Name switch
        {
            nameof(TutorialSlime) => 766,   // slime
            nameof(LoopedSlime) => 766,     // the same, on a later lap
            nameof(Skelly) => 881,          // bone fragment
            nameof(ThunderKid) => 787,      // battery pack
            nameof(Dragon) => 852,          // dragon tooth
            nameof(BigBug) => 684,          // bug meat
            nameof(QueenBee) => 340,        // honey
            nameof(ShadowKing) => 769,      // void essence
            _ => 820                        // fossilised skull
        };

        private static void DrawThread(SpriteBatch b, Vector2 from, Vector2 to, Color colour, int thickness)
        {
            Vector2 delta = to - from;
            float length = delta.Length();
            if (length < 1f)
                return;

            b.Draw(
                Game1.staminaRect, from, null, colour,
                (float)Math.Atan2(delta.Y, delta.X), Vector2.Zero,
                new Vector2(length, thickness), SpriteEffects.None, 0f);
        }

        /// <summary>Where the nth of a line's forks stands.</summary>
        private Rectangle NodeBounds(int level, int index, int count)
        {
            int size = Known(level)
                ? (BossFloor.IsBossFloor(level) ? BossNodeSize : NodeSize)
                : UnknownNodeSize;

            float offset = index - (count - 1) / 2f;
            int x = roadCentreX + (int)(offset * SlotWidth);
            int y = (TopOf(level) ?? roadTop) + RowHeightFor(level) / 2;

            return new Rectangle(x - size / 2, y - size / 2, size, size);
        }

        private void DrawNode(SpriteBatch b, int level, FloorKind? kind, bool taken, Rectangle bounds)
        {
            bool here = level == Roguelike.CurrentLevel && taken;
            bool behind = level < Roguelike.CurrentLevel;

            // A fork the group did not walk is drawn but not lit: it says what the
            // choice was, and should not compete with what was chosen.
            float alpha = taken ? (here ? 1f : (behind ? 0.95f : 0.6f)) : 0.4f;

            if (here)
            {
                drawTextureBox(
                    b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                    bounds.X - 8, bounds.Y - 8, bounds.Width + 16, bounds.Height + 16,
                    new Color(255, 196, 92), drawShadow: false);
            }

            if (kind is null)
            {
                // A plain stone on the road. Drawn as four nested rectangles rather
                // than a box, because the frame art has a corner of its own at every
                // size and at twenty pixels a node is all corner.
                b.Draw(Game1.staminaRect, bounds, RoadColour * 0.55f);
                b.Draw(
                    Game1.staminaRect,
                    new Rectangle(bounds.X + 3, bounds.Y + 3, bounds.Width - 6, bounds.Height - 6),
                    new Color(226, 186, 128));
                return;
            }

            drawTextureBox(
                b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                bounds.X, bounds.Y, bounds.Width, bounds.Height,
                Color.White * (taken ? 1f : 0.55f), drawShadow: false);

            Texture2D texture;
            Rectangle source;

            if (kind == FloorKind.Boss)
            {
                texture = Game1.objectSpriteSheet;
                source = Game1.getSourceRectForStandardTileSheet(
                    Game1.objectSpriteSheet, BossToken(BossManager.BossTypeFor(level)), 16, 16);
            }
            else
            {
                texture = FloorKindInfo.IconTexture(kind.Value);
                source = FloorKindInfo.Icon(kind.Value);
            }

            float scale = (bounds.Width - 20f) / Math.Max(source.Width, source.Height);

            b.Draw(
                texture,
                new Vector2(
                    bounds.X + (bounds.Width - source.Width * scale) / 2f,
                    bounds.Y + (bounds.Height - source.Height * scale) / 2f),
                source, Color.White * alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 1f);
        }

        /// <summary>Draws what a floor gave the player, out in the right-hand margin.</summary>
        private void DrawMarks(SpriteBatch b, int level)
        {
            IReadOnlyList<RunLog.Entry> entries = RunLog.For(level);
            if (entries.Count == 0)
                return;

            int x = panelX + panelWidth - ScrollGutter - MarkGutter + 4;
            int y = (TopOf(level) ?? roadTop) + (RowHeightFor(level) - MarkSize) / 2;

            foreach (RunLog.Entry entry in entries)
            {
                Rectangle bounds = new(x, y, MarkSize, MarkSize);

                if (entry.Kind == RunLog.EventKind.PerkTaken)
                {
                    Perks.PerkType perk = (Perks.PerkType)entry.Which;
                    Rectangle source = Perks.GetPerkSourceRect(perk);

                    b.Draw(
                        Perks.GetPerkIconTexture(perk), new Vector2(bounds.X, bounds.Y), source,
                        Color.White, 0f, Vector2.Zero,
                        MarkSize / (float)Math.Max(source.Width, source.Height),
                        SpriteEffects.None, 1f);

                    marks.Add((
                        bounds,
                        Perks.GetPerkDisplayName(perk) + " " + Numeral(entry.Depth),
                        Perks.GetPerkDescription(perk, entry.Depth)));
                }
                else
                {
                    bool lifted = entry.Kind == RunLog.EventKind.CurseLifted;
                    CurseType curse = (CurseType)entry.Which;

                    b.Draw(
                        Game1.buffsIcons, new Vector2(bounds.X, bounds.Y),
                        Game1.getSourceRectForStandardTileSheet(Game1.buffsIcons, Curse.IconId, 16, 16),
                        lifted ? Color.White * 0.45f : new Color(214, 120, 120),
                        0f, Vector2.Zero, MarkSize / 16f, SpriteEffects.None, 1f);

                    marks.Add((
                        bounds,
                        Curse.GetCurseDisplayName(curse),
                        lifted ? I18n.UI_RunMap_CurseLifted() : I18n.UI_RunMap_CurseGained()));
                }

                x += MarkSize + MarkGap;
            }
        }

        private static string Numeral(int level) => level switch
        {
            1 => "I",
            2 => "II",
            3 => "III",
            4 => "IV",
            _ => level.ToString()
        };

        public override void performHoverAction(int x, int y)
        {
            hover = "";
            hoverTitle = "";

            upperRightCloseButton.tryHover(x, y);
            upArrow.tryHover(x, y);
            downArrow.tryHover(x, y);

            foreach ((Rectangle bounds, int level, FloorKind? kind, bool taken) in drawn)
            {
                if (!bounds.Contains(x, y))
                    continue;

                hoverTitle = I18n.UI_RunMap_Floor(level: level);
                hover = Describe(level, kind, taken);
                return;
            }

            foreach ((Rectangle bounds, string title, string text) in marks)
            {
                if (!bounds.Contains(x, y))
                    continue;

                hoverTitle = title;
                hover = text;
                return;
            }
        }

        /// <summary>
        /// What to say about a node under the cursor.
        /// </summary>
        /// <remarks>
        /// A boss keeps its name until the player has stood on its floor. The trophy
        /// still says which one it is to anybody who has met it before, which is
        /// knowledge earned across runs rather than handed over by the map.
        /// </remarks>
        private static string Describe(int level, FloorKind? kind, bool taken)
        {
            if (kind is null)
                return I18n.UI_RunMap_NotReached();

            if (kind == FloorKind.Boss)
            {
                Type? boss = BossManager.BossTypeFor(level);

                return Reached(level) && boss is not null
                    ? BossManager.DisplayNameFor(boss)
                    : I18n.UI_RunMap_UnknownBoss();
            }

            string name = FloorKindInfo.Name(kind.Value);

            return taken ? name : I18n.UI_RunMap_NotTaken(kind: name);
        }

        /// <summary>Where the fork the group actually walked stands, on its line.</summary>
        private Rectangle Chosen(int level)
        {
            FloorKind[] forks = Forks(level);
            FloorKind? chosen = Taken(level);

            for (int i = 0; i < forks.Length; i++)
            {
                if (forks[i] == chosen)
                    return NodeBounds(level, i, forks.Length);
            }

            return NodeBounds(level, 0, Math.Max(1, forks.Length));
        }

        public override void draw(SpriteBatch b)
        {
            drawn.Clear();
            marks.Clear();

            // GameMenu skips its own dimming for this tab, and a page drawn straight
            // onto the mine floor is unreadable.
            b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.5f);

            drawTextureBox(
                b, Game1.menuTexture, new Rectangle(0, 256, 60, 60),
                panelX, yPositionOnScreen, panelWidth, height, Color.White, drawShadow: true);

            Vector2 titleSize = Game1.dialogueFont.MeasureString(I18n.UI_RunMap_Title());
            Utility.drawTextWithShadow(
                b, I18n.UI_RunMap_Title(), Game1.dialogueFont,
                new Vector2(panelX + panelWidth / 2 - titleSize.X / 2, yPositionOnScreen + 28),
                Game1.textColor);

            BuildWindow();

            // The road first, so every node sits on top of it. A line runs from the
            // floor above down to each fork of the exit below, so an exit that
            // offered three ways reads as three ways.
            foreach ((int level, int _, int _) in window)
            {
                if (TopOf(level + 1) is null)
                    continue;

                Vector2 above = Taken(level) is null
                    ? NodeBounds(level, 0, 1).Center.ToVector2()
                    : Chosen(level).Center.ToVector2();

                FloorKind[] forks = Forks(level + 1);
                FloorKind? chosen = Taken(level + 1);

                if (forks.Length == 0)
                {
                    DrawThread(
                        b, above, NodeBounds(level + 1, 0, 1).Center.ToVector2(),
                        RoadColour * 0.3f, 3);
                    continue;
                }

                // Only to the fork that was taken. The others are still drawn as
                // nodes, because what the exit offered is worth remembering, but a
                // road to a floor nobody walked is a road that does not exist - and
                // three of them from every floor turned the page into a lattice.
                for (int i = 0; i < forks.Length; i++)
                {
                    if (forks[i] != chosen)
                        continue;

                    DrawThread(
                        b, above, NodeBounds(level + 1, i, forks.Length).Center.ToVector2(),
                        RoadColour, ThreadThickness);
                }
            }

            foreach ((int level, int top, int rowHeight) in window)
            {
                string number = level.ToString();
                Vector2 numberSize = Game1.smallFont.MeasureString(number);

                Utility.drawTextWithShadow(
                    b, number, Game1.smallFont,
                    new Vector2(
                        panelX + NumberGutter - numberSize.X - 4,
                        top + (rowHeight - numberSize.Y) / 2),
                    level == Roguelike.CurrentLevel ? Game1.textColor : MutedColour);

                FloorKind[] forks = Forks(level);
                FloorKind? chosen = Taken(level);

                if (forks.Length == 0)
                {
                    Rectangle bounds = NodeBounds(level, 0, 1);
                    DrawNode(b, level, null, taken: true, bounds);
                    drawn.Add((bounds, level, null, true));
                }
                else
                {
                    for (int i = 0; i < forks.Length; i++)
                    {
                        Rectangle bounds = NodeBounds(level, i, forks.Length);
                        bool taken = forks[i] == chosen;

                        DrawNode(b, level, forks[i], taken, bounds);
                        drawn.Add((bounds, level, forks[i], taken));
                    }
                }

                DrawMarks(b, level);
            }

            if (firstFloor > 1)
                upArrow.draw(b);

            if (window.Count > 0 && window[^1].Level < LastFloor)
                downArrow.draw(b);

            upperRightCloseButton.draw(b);

            if (hoverTitle.Length > 0)
                drawHoverText(b, hover, Game1.smallFont, 0, 0, -1, hoverTitle);

            drawMouse(b);
        }
    }
}

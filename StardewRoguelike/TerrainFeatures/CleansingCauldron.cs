using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace StardewRoguelike.TerrainFeatures
{
    internal class CleansingCauldron : TerrainFeature
    {
        /// <summary>The cauldron with something in it.</summary>
        private const string FullTexture = "TerrainFeatures\\CleansingCauldron";

        /// <summary>The cauldron with nothing in it.</summary>
        private const string EmptyTexture = "TerrainFeatures\\CleansingCauldronEmpty";

        private readonly Lazy<Texture2D> fullTexture = new(() => Game1.content.Load<Texture2D>(FullTexture));

        private readonly Lazy<Texture2D> emptyTexture = new(() => Game1.content.Load<Texture2D>(EmptyTexture));

        /// <summary>The players who have already had a curse taken off them here.</summary>
        /// <remarks>
        /// A set of players rather than one flag, for the reason the shrine keeps one:
        /// a curse hangs off a single farmer, so cleansing is a player's own reward,
        /// and one bool made it one cleansing for the floor. In split-screen, where
        /// both local players share this object, the first to use it took the second
        /// player's turn with it.
        ///
        /// Local on purpose, not a net field - see <see cref="PerkShrine.usedBy"/>.
        /// </remarks>
        private readonly HashSet<long> usedBy = new();

        /// <summary>Whether the player looking at it has already used it.</summary>
        private bool UsedByMe => usedBy.Contains(Game1.player.UniqueMultiplayerID);

        /// <summary>The players who have been told the cauldron filled.</summary>
        private readonly HashSet<long> announcedTo = new();

        /// <summary>Ticks so it can notice the last monster dying.</summary>
        public CleansingCauldron() : base(needsTick: true) { }

        /// <summary>
        /// Whether the cauldron will take a curse.
        ///
        /// The floor has to be cleared first, the same as the shrine's. Undoing a
        /// decision is worth a floor of work; otherwise a cauldron is a free walk to
        /// a pot, and the curse the shrine charged stops being a price at all.
        /// </summary>
        private bool Ready => !UsedByMe && Location is MineShaft mine && mine.EnemyCount == 0;

        public override bool tickUpdate(GameTime time)
        {
            // Said once, when the last monster dies, and the pot fills to match. The
            // colour is the only thing telling the player it is waiting on anything.
            if (Ready && announcedTo.Add(Game1.player.UniqueMultiplayerID))
            {
                Game1.chatBox?.addMessage(I18n.Cauldron_Ready(), Color.Gold);
                Location?.localSound("bubbles");
            }

            return false;
        }

        public override Rectangle getBoundingBox()
        {
            return new((int)Tile.X * 64, (int)(Tile.Y + 1) * 64, 192, 128);
        }

        /// <summary>
        /// Puts a cauldron on <paramref name="mine"/>, near where the player arrives.
        ///
        /// In the middle of the floor, like the shrine, and for the same reason: the
        /// floor has to be cleared before it will take anything, so the middle is
        /// where the fight ends up anyway.
        /// </summary>
        public static void PlaceIn(MineShaft mine)
        {
            if (mine.map is null)
                return;

            Vector2 middle = new(
                mine.Map.Layers[0].LayerWidth / 2,
                mine.Map.Layers[0].LayerHeight / 2);

            if (TryPlace(mine, middle))
                return;

            // Nothing clear in the middle still gets a cauldron rather than none,
            // since the floor was chosen for it.
            TryPlace(mine, mine.tileBeneathLadder);
        }

        private static bool TryPlace(MineShaft mine, Vector2 entrance)
        {
            for (int radius = 0; radius <= 12; radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        if (radius > 0 && Math.Abs(dx) != radius && Math.Abs(dy) != radius)
                            continue;

                        Vector2 tile = new(entrance.X + dx, entrance.Y + dy);
                        if (!HasRoomFor(mine, tile))
                            continue;

                        mine.terrainFeatures.Add(tile, new CleansingCauldron());
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Whether a cauldron fits here.
        ///
        /// It is drawn three tiles wide and two tall, so the tiles around it have to
        /// be clear of anything else that stands on the floor.
        /// </summary>
        private static bool HasRoomFor(MineShaft mine, Vector2 tile)
        {
            for (int dx = 0; dx < 3; dx++)
            {
                for (int dy = 0; dy < 2; dy++)
                {
                    Vector2 part = new(tile.X + dx, tile.Y + dy);

                    if (mine.terrainFeatures.ContainsKey(part) || !mine.isTileClearForMineObjects(part))
                        return false;
                }
            }

            return true;
        }

        public override bool performUseAction(Vector2 tileLocation)
        {
            if (UsedByMe)
                return false;

            if (!Ready)
            {
                // A line in the chat rather than a dialogue box, so being told no
                // does not stop the floor while something is still chasing you.
                Game1.chatBox?.addMessage(I18n.Cauldron_Dormant(), Color.Gold);
                return false;
            }

            if (!Curse.HasAnyCurse())
            {
                Game1.drawObjectDialogue(I18n.Cauldron_NothingToTake());
                return false;
            }

            // Cleansing, and nothing else. It handed over a perk as well while it
            // was a room behind one of two doors, on a floor nobody could count on
            // reaching. As a floor of its own that is two rewards for no price, and
            // it would be strictly better than the shrine it is meant to be the
            // other half of: the shrine sells a perk for a curse, this buys the
            // curse back, and a run has to decide which it wants a floor for.
            //
            // Which curse it takes is the player's call. It used to take one at
            // random, which made the cauldron a floor spent on a dice roll - and the
            // shrine charges deliberately now, so undoing it blind was the one half
            // of the pair still deciding something important on the player's behalf.
            List<Response> responses = new();
            foreach (CurseType curse in Curse.HeldCurses())
            {
                responses.Add(new Response(
                    ((int)curse).ToString(),
                    Curse.GetCurseDisplayName(curse)));
            }

            // Walking away has to be possible: the pot is spent on use, so a player
            // who opened it to read what they were carrying should not lose the floor
            // for it.
            responses.Add(new Response(CancelKey, I18n.UI_Cancel()));

            pending.Value = this;
            Location?.createQuestionDialogue(
                I18n.Cauldron_Choose(), responses.ToArray(), QuestionKey);

            return true;
        }

        /// <summary>The key the cauldron's question is asked under.</summary>
        private const string QuestionKey = "cauldronCleanse";

        /// <summary>The response that takes nothing and spends nothing.</summary>
        private const string CancelKey = "none";

        /// <summary>
        /// The cauldron whose question is open on this screen.
        ///
        /// A dialogue answer arrives through the location rather than through the
        /// thing that asked, so the cauldron has to be remembered to be marked spent.
        /// Per screen, because two local players can each have their own question up.
        /// </summary>
        private static readonly PerScreen<CleansingCauldron?> pending = new(() => null);

        /// <summary>Takes the curse the player picked out of the pot.</summary>
        public static bool AnswerDialogueAction(string questionAndAnswer)
        {
            if (!questionAndAnswer.StartsWith($"{QuestionKey}_"))
                return false;

            CleansingCauldron? cauldron = pending.Value;
            pending.Value = null;

            string answer = questionAndAnswer[(QuestionKey.Length + 1)..];
            if (answer == CancelKey || cauldron is null)
                return true;

            if (!int.TryParse(answer, out int id) || !Enum.IsDefined(typeof(CurseType), id))
                return true;

            CurseType curse = (CurseType)id;

            // Re-checked rather than trusted: the answer arrives a frame or more after
            // the question, and anything that can take a curse off in between - another
            // local player's pot, a run reset - would leave this removing nothing and
            // spending the floor for it.
            if (!Curse.HasCurse(curse))
                return true;

            cauldron.usedBy.Add(Game1.player.UniqueMultiplayerID);
            Game1.playSound("glug");
            Curse.RemoveCurse(curse);

            Game1.chatBox?.addMessage(
                I18n.Cauldron_Cleansed(curse: Curse.GetCurseDisplayName(curse)),
                Color.Gold);

            return true;
        }

        public override bool isPassable(Character? c = null)
        {
            return false;
        }

        public override bool isActionable()
        {
            return false;
        }

        public override void draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(Ready ? fullTexture.Value : emptyTexture.Value, Game1.GlobalToLocal(Game1.viewport, Tile * 64f), new Rectangle(0, 0, 48, 48), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, (Tile.Y + 2f) * 64f / 10000f);
        }
    }
}

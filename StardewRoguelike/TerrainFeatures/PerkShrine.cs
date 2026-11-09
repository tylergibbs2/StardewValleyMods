using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewRoguelike.UI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;

namespace StardewRoguelike.TerrainFeatures
{
    /// <summary>
    /// Offers power for a price: take a curse, choose a perk.
    ///
    /// The deliberate inverse of <see cref="CleansingCauldron"/>, which spends a
    /// curse to grant a perk. Between them a curse is a currency rather than only a
    /// penalty - the shrine sells you one, the cauldron buys it back - and perks stop
    /// arriving on a fixed timetable, since reaching a shrine is a routing decision.
    /// </summary>
    internal class PerkShrine : TerrainFeature
    {
        /// <summary>
        /// The Wicked Statue, drawn straight from the game's big craftable sheet.
        ///
        /// Vanilla art rather than anything the mod ships: it is already the game's
        /// own dark statue, which is exactly what a shrine selling power for a curse
        /// should look like, and it means no new sprite to draw or asset to load. The
        /// mod's other statues work the same way - the healing bear on a pick-a-path
        /// floor is built from townInterior tiles.
        /// </summary>
        private const int WickedStatue = 83;

        /// <summary>The players who have already traded here.</summary>
        /// <remarks>
        /// A set of players rather than one flag, because what the shrine hands over
        /// is a player's own: perks are per screen, curses hang off each farmer, and
        /// <see cref="Offer"/> is computed from the perks of whoever is asking - two
        /// players at this statue are offered different pairs. A single bool made it
        /// one trade for the floor, so in split-screen the first player to use it
        /// locked the second out of an offer that had been worked out for them.
        ///
        /// Deliberately not a net field. Each player's entitlement is their own
        /// business, so there is nothing here the other machines need to agree about,
        /// and keeping it local is also what makes the statue look spent to the
        /// player who spent it and ready to the player who has not.
        /// </remarks>
        private readonly HashSet<long> usedBy = new();

        /// <summary>Whether the player looking at it has already traded here.</summary>
        private bool UsedByMe => usedBy.Contains(Game1.player.UniqueMultiplayerID);

        /// <summary>The level this shrine stands on, which names what it offers.</summary>
        private int Level => Location is MineShaft mine ? Roguelike.GetLevelFromMineshaft(mine) : 0;

        /// <summary>The two perks this shrine is offering this player.</summary>
        /// <remarks>
        /// Asked of the plan rather than rolled, so it is the same pair the exit menu
        /// named on the floor above - and the same pair however many times the menu is
        /// opened and closed.
        /// </remarks>
        public (Perks.PerkType? Left, Perks.PerkType? Right) Offer => Perks.GetShrineOffer(Level);

        /// <summary>The players who have been told the shrine woke up.</summary>
        /// <remarks>
        /// Per player for the same reason, and because the chat box it writes to is:
        /// one flag on a statue two local players share announced it to whichever
        /// screen happened to tick first and left the other wondering.
        /// </remarks>
        private readonly HashSet<long> announcedTo = new();

        /// <summary>Ticks so it can notice the last monster dying.</summary>
        public PerkShrine() : base(needsTick: true) { }

        /// <summary>
        /// Whether the shrine will trade.
        ///
        /// The floor has to be cleared first, so a shrine is the reward for the fight
        /// rather than something to run past on the way to the exit.
        /// </summary>
        private bool Ready => Location is MineShaft mine && mine.EnemyCount == 0;

        /// <summary>Whether this player can still trade here, and the floor is clear.</summary>
        private bool ReadyForMe => !UsedByMe && Ready;

        public override bool tickUpdate(GameTime time)
        {
            // Said once, when the last monster dies, so the player is not left
            // guessing why the statue has changed colour.
            if (ReadyForMe && announcedTo.Add(Game1.player.UniqueMultiplayerID))
            {
                Game1.chatBox?.addMessage(I18n.Shrine_Ready(), Color.Gold);
                Location?.localSound("secret1");
            }

            return false;
        }

        public override Rectangle getBoundingBox()
        {
            return new((int)Tile.X * 64, (int)Tile.Y * 64, 64, 64);
        }

        /// <summary>How far from an origin tile a shrine will be looked for.</summary>
        private const int SearchRadius = 12;

        /// <summary>
        /// Puts a shrine on <paramref name="mine"/>, in the middle of the floor.
        ///
        /// It used to stand by the entrance, which was right while the floor could be
        /// walked straight past. Now that a shrine floor has to be cleared to get off
        /// it, the middle is where the fight ends up anyway, and a statue standing in
        /// the middle of the floor is something to walk towards rather than something
        /// passed on the way in.
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

            // A layout with nothing clear in the middle still gets a shrine, rather
            // than none, since the floor was chosen for it.
            TryPlace(mine, mine.tileBeneathLadder);
        }

        /// <summary>Looks outward from <paramref name="origin"/> for somewhere to stand.</summary>
        private static bool TryPlace(MineShaft mine, Vector2 origin)
        {
            for (int radius = 0; radius <= SearchRadius; radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        // Only the edge of each ring, so the search spreads outward
                        // rather than re-checking everything inside it.
                        if (radius > 0 && Math.Abs(dx) != radius && Math.Abs(dy) != radius)
                            continue;

                        Vector2 tile = new(origin.X + dx, origin.Y + dy);
                        if (!HasRoomFor(mine, tile))
                            continue;

                        mine.terrainFeatures.Add(tile, new PerkShrine());
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Whether the shrine fits at this tile.
        ///
        /// It stands on one tile and is drawn two tall, so the tile above only has to
        /// be free of another shrine, not walkable.
        /// </summary>
        private static bool HasRoomFor(MineShaft mine, Vector2 tile)
        {
            if (mine.terrainFeatures.ContainsKey(tile))
                return false;

            if (!mine.isTileClearForMineObjects(tile))
                return false;

            return !mine.terrainFeatures.ContainsKey(new Vector2(tile.X, tile.Y - 1));
        }

        public override bool performUseAction(Vector2 tileLocation)
        {
            if (UsedByMe)
                return false;

            if (!Ready)
            {
                // A line in the chat rather than a dialogue box: being told no should
                // not stop the floor while something is still chasing you.
                Game1.chatBox?.addMessage(I18n.Shrine_Dormant(), Color.Gold);
                return false;
            }

            if (Perks.HasAllPerks())
            {
                Game1.drawObjectDialogue(I18n.Shrine_NothingToOffer());
                return false;
            }

            // A choice rather than a random grant, which is what the merchant floors
            // used to give and is the more interesting half of taking a perk.
            //
            // The pair comes from the plan, so it is what the exit menu promised and
            // does not change if the player closes this and opens it again.
            (Perks.PerkType? left, Perks.PerkType? right) = Offer;

            // Each perk is shown with the curse it costs, so the two sides of the
            // shrine are two bargains rather than one choice and one dice roll.
            PerkMenu menu = new(
                left, right,
                left.HasValue ? Curse.CurseFor(Level, left.Value) : null,
                right.HasValue ? Curse.CurseFor(Level, right.Value) : null);

            menu.isActive = true;
            menu.informationUp = true;
            menu.OnPerkChosen = TakePayment;

            Perks.CurrentMenu = menu;
            Game1.activeClickableMenu = menu;

            return true;
        }

        /// <summary>
        /// Takes the curse, once a perk has actually been taken.
        ///
        /// Charged here rather than when the menu opens. It used to be taken up
        /// front, so closing the menu without choosing left the player cursed with
        /// nothing to show for it, and the shrine - marked spent at the same moment -
        /// would not open again to make good on it.
        /// </summary>
        private void TakePayment(Perks.PerkType chosen)
        {
            usedBy.Add(Game1.player.UniqueMultiplayerID);
            Game1.playSound("debuffSpell");

            // The curse the menu named beside the perk that was taken, rather than a
            // fresh roll: the price was part of the offer, so charging anything else
            // would make the thing the player read and weighed a decoration.
            //
            // Running out of curses to give does not block the trade: the player has
            // already paid for every one they are carrying.
            CurseType? curse = Curse.CurseFor(Level, chosen);
            if (!curse.HasValue)
                return;

            Curse.AddCurse(curse.Value);

            // Said out loud. The curse was being applied silently and the perk menu
            // drawn over the top of it, so the price went unseen and the shrine
            // looked like it was giving perks away.
            Game1.chatBox?.addMessage(
                I18n.Shrine_Cursed(curse: Curse.GetCurseDisplayName(curse.Value)),
                Color.OrangeRed);
        }

        /// <summary>
        /// Dark while it will not trade, lit once it will, grey once it has.
        ///
        /// The colour is the only thing telling the player the shrine is waiting on
        /// something, so the dormant state is clearly darker rather than subtly so.
        /// </summary>
        private Color Tint()
        {
            if (UsedByMe)
                return Color.Gray * 0.55f;

            if (!Ready)
                return Color.SlateGray * 0.7f;

            return Color.White;
        }

        public override bool isPassable(Character? c = null)
        {
            return false;
        }

        public override bool isActionable()
        {
            return !UsedByMe;
        }

        public override void draw(SpriteBatch spriteBatch)
        {
            // Drawn from the tile above, since a big craftable is two tiles tall and
            // stands on the lower one.
            Vector2 position = Game1.GlobalToLocal(
                Game1.viewport, new Vector2(Tile.X * 64f, (Tile.Y - 1) * 64f));

            spriteBatch.Draw(
                Game1.bigCraftableSpriteSheet,
                position,
                StardewValley.Object.getSourceRectForBigCraftable(WickedStatue),
                Tint(),
                0f,
                Vector2.Zero,
                4f,
                SpriteEffects.None,
                (Tile.Y * 64f + 32f) / 10000f);
        }
    }
}

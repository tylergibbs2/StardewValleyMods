using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using System.Collections.Generic;

namespace StardewRoguelike
{
    /// <summary>What a treasure chest asks for before it opens.</summary>
    public enum ChestPrice
    {
        /// <summary>A curse, which the cauldron floors exist to take back.</summary>
        Curse,

        /// <summary>A share of the player's maximum health, taken at once.</summary>
        Health,

        /// <summary>Gold, which has had nowhere to go between merchants.</summary>
        Gold
    }

    /// <summary>
    /// A chest that wants something before it opens.
    ///
    /// Treasure was the only quiet floor that always paid and never asked, which
    /// made it the strictly best of them - the only reason not to take it every
    /// cycle was the rule saying one quiet floor per cycle. It holds something
    /// worth having again, and now it charges for it, so the decision is whether
    /// the thing inside is worth what it wants rather than whether the roll was
    /// kind.
    /// </summary>
    internal class PricedChest : Chest
    {
        private readonly ChestPrice price;

        private readonly int gold;

        /// <summary>Whether the price has been paid, after which it is a chest.</summary>
        private bool paid;

        // No initNetFields() here: Item's constructor already calls it virtually.
        public PricedChest() : base() { }

        public PricedChest(List<Item> items, Vector2 tileLocation, ChestPrice price, int gold)
            : base(items, tileLocation)
        {
            this.price = price;
            this.gold = gold;
        }

        public override bool checkForAction(Farmer who, bool justCheckingForActivity = false)
        {
            if (justCheckingForActivity || paid)
                return base.checkForAction(who, justCheckingForActivity);

            Ask(who);
            return true;
        }

        /// <summary>
        /// Puts the price to the player, and opens on a yes.
        /// </summary>
        /// <remarks>
        /// Asked rather than taken. The exit menu named the price a floor ago, so
        /// nothing here is news - but a chest that charged the moment it was walked
        /// into would make the walk the decision, and the walk is not where the
        /// player is thinking about it.
        /// </remarks>
        private void Ask(Farmer who)
        {
            GameLocation location = who.currentLocation;

            string question = price switch
            {
                ChestPrice.Curse => I18n.Chest_AskCurse(),
                ChestPrice.Health => I18n.Chest_AskHealth(amount: HealthCost(who)),
                _ => I18n.Chest_AskGold(amount: gold)
            };

            Response[] responses =
            {
                new("Yes", Game1.content.LoadString("Strings\\Lexicon:QuestionDialogue_Yes")),
                new("No", Game1.content.LoadString("Strings\\Lexicon:QuestionDialogue_No"))
            };

            location.createQuestionDialogue(question, responses, (farmer, answer) =>
            {
                if (answer != "Yes")
                    return;

                if (!Pay(farmer))
                    return;

                paid = true;
                base.checkForAction(farmer);
            });
        }

        /// <summary>Takes the price, or says why it cannot be taken.</summary>
        private bool Pay(Farmer who)
        {
            switch (price)
            {
                case ChestPrice.Gold:
                    if (who.Money < gold)
                    {
                        Game1.drawObjectDialogue(I18n.Chest_NotEnoughGold());
                        return false;
                    }

                    who.Money -= gold;
                    Game1.playSound("purchaseClick");
                    return true;

                case ChestPrice.Health:
                    int cost = HealthCost(who);

                    if (who.health <= cost)
                    {
                        Game1.drawObjectDialogue(I18n.Chest_NotEnoughHealth());
                        return false;
                    }

                    who.health -= cost;
                    Game1.playSound("ow");
                    return true;

                default:
                    CurseType? curse = Curse.GetRandomUniqueCurse();

                    // Nothing left to give does not block the trade: the player is
                    // already carrying every curse there is.
                    if (curse.HasValue)
                    {
                        Curse.AddCurse(curse.Value);
                        Game1.chatBox?.addMessage(
                            I18n.Chest_Cursed(curse: Curse.GetCurseDisplayName(curse.Value)),
                            Color.OrangeRed);
                    }

                    Game1.playSound("debuffSpell");
                    return true;
            }
        }

        /// <summary>A third of what the player can take, rounded down.</summary>
        public static int HealthCost(Farmer who)
        {
            return who.maxHealth / 3;
        }
    }
}

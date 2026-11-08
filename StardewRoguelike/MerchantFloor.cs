using StardewValley;
using StardewValley.Extensions;
using StardewValley.Objects;
using StardewValley.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StardewRoguelike
{
    public class MerchantFloor
    {
        public readonly int SwordsToPick;
        public readonly int RingsToPick;
        public readonly int SpecialRingsToPick;
        public readonly int BootsToPick;
        public readonly int SpecialFoodToPick;

        // Dict key is item id, tuple is (minPrice, maxPrice, quantity)
        public readonly Dictionary<string, Tuple<int, int, int>> Swords = new();
        public readonly Dictionary<string, Tuple<int, int, int>> Rings = new();
        public readonly Dictionary<string, Tuple<int, int, int>> SpecialRings = new();
        public readonly Dictionary<string, Tuple<int, int, int>> Boots = new();
        public readonly Dictionary<string, Tuple<int, int, int>> SpecialFood = new();

        public MerchantFloor() { }

        public MerchantFloor(int swordsToPick, int ringsToPick, int specialRingsToPick, int bootsToPick, int specialFoodToPick)
        {
            SwordsToPick = swordsToPick;
            RingsToPick = ringsToPick;
            SpecialRingsToPick = specialRingsToPick;
            BootsToPick = bootsToPick;
            SpecialFoodToPick = specialFoodToPick;
        }

        public void AddSword(string id, int priceFrom, int priceTo, int quantity=-1)
        {
            Swords[id] = new(priceFrom, priceTo, quantity);
        }

        public void AddRing(string id, int priceFrom, int priceTo, int quantity = -1)
        {
            Rings[id] = new(priceFrom, priceTo, quantity);
        }

        public void AddSpecialRing(string id, int priceFrom, int priceTo, int quantity = -1)
        {
            SpecialRings[id] = new(priceFrom, priceTo, quantity);
        }

        public void AddBoots(string id, int priceFrom, int priceTo, int quantity = -1)
        {
            Boots[id] = new(priceFrom, priceTo, quantity);
        }

        public void AddSpecialFood(string id, int priceFrom, int priceTo, int quantity = -1)
        {
            SpecialFood[id] = new(priceFrom, priceTo, quantity);
        }

        public static List<T> PickNFromList<T>(IList<T> source, int numToPick, Random? random = null)
        {
            random ??= Game1.random;

            // A copy, and never a cast. Callers pass any IList<T>: EggHunt hands over
            // a NetCollection<Egg>, which this cast would have thrown on the moment
            // it held fewer eggs than the three asked for. Returning the caller's own
            // list was wrong even when the cast happened to succeed, since whoever
            // got it back could then mutate the source.
            if (numToPick > source.Count)
                return source.ToList();

            List<T> pickedElements = new();
            while (pickedElements.Count < numToPick)
            {
                int pickedIndex = random.Next(source.Count);
                if (pickedElements.Contains(source[pickedIndex]))
                    continue;

                pickedElements.Add(source[pickedIndex]);
            }

            return pickedElements;
        }

        public Item PickAnyRandom(Random? random = null)
        {
            random ??= Game1.random;

            List<Item> validItems = new();

            foreach (string swordId in Swords.Keys)
                validItems.Add(new MeleeWeapon(swordId));

            foreach (string ringId in Rings.Keys)
                validItems.Add(new Ring(ringId));

            foreach (string ringId in SpecialRings.Keys)
                validItems.Add(new Ring(ringId));

            foreach (string bootId in Boots.Keys)
                validItems.Add(new Boots(bootId));

            foreach (string specialFoodId in SpecialFood.Keys)
                validItems.Add(new SObject(specialFoodId, 1));

            return random.ChooseFrom(validItems);
        }

        public Item PickAnyRandomAvoiding(List<string> toAvoid, Random? random = null)
        {
            random ??= Game1.random;

            List<Item> validItems = new();

            foreach (string swordId in Swords.Keys)
                validItems.Add(new MeleeWeapon(swordId));

            foreach (string ringId in Rings.Keys)
                validItems.Add(new Ring(ringId));

            foreach (string ringId in SpecialRings.Keys)
                validItems.Add(new Ring(ringId));

            foreach (string bootId in Boots.Keys)
                validItems.Add(new Boots(bootId));

            foreach (string specialFoodId in SpecialFood.Keys)
                validItems.Add(new SObject(specialFoodId, 1));

            while (toAvoid.Count >= validItems.Count)
                toAvoid.RemoveAt(Game1.random.Next(toAvoid.Count));

            validItems.RemoveAll(item => toAvoid.Contains(item.DisplayName));

            return random.ChooseFrom(validItems);
        }

        public MeleeWeapon PickAnySword(Random? random = null)
        {
            random ??= Game1.random;
            string swordId = random.ChooseFrom(PickNFromList(Swords.Keys.ToList(), SwordsToPick));
            return new(swordId);
        }

        public Ring PickAnyRing(Random? random = null)
        {
            random ??= Game1.random;
            var allRingIds = Rings.Keys.ToList().Concat(SpecialRings.Keys.ToList()).ToList();
            string ringId = random.ChooseFrom(PickNFromList(allRingIds, RingsToPick + SpecialRingsToPick));
            return new(ringId);
        }

        public Boots PickAnyBoots(Random? random = null)
        {
            random ??= Game1.random;
            string bootId = random.ChooseFrom(PickNFromList(Boots.Keys.ToList(), BootsToPick));
            return new(bootId);
        }

        public SObject PickAnyFood(Random? random = null)
        {
            random ??= Game1.random;
            List<string> regularFoodIds = new() { "194", "196", "773" };
            var allFoodIds = SpecialFood.Keys.ToList().Concat(regularFoodIds).ToList();
            string specialFoodId = random.ChooseFrom(PickNFromList(allFoodIds, SpecialFoodToPick + 3, random));
            return new(specialFoodId, 1);
        }


        public static int Randint(int from, int to, Random? random = null)
        {
            random ??= Game1.random;
            return random.Next(from, to + 1);
        }

        public static int CalculateBuyPrice(int priceFrom, int priceTo, float priceAdjustment = 1f, Random? random = null)
        {
            random ??= Game1.random;
            int result;
            if (Curse.HasCurse(CurseType.CheaperMerchant))
                result = Randint((int)Math.Round(priceFrom * 0.6f), (int)Math.Round(priceTo * 0.6f), random);
            else
                result = Randint(priceFrom, priceTo, random);

            return (int)(result * priceAdjustment);
        }

        public void AddToStock(Dictionary<ISalable, ItemStockInformation> stock, float priceAdjustment = 1f, Random? random = null)
        {
            random ??= Game1.random;

            int swordsToPick = SwordsToPick;
            int ringsToPick = RingsToPick;
            int specialRingsToPick = SpecialRingsToPick;
            int bootsToPick = BootsToPick;
            int specialFoodToPick = SpecialFoodToPick;

            if (Curse.HasCurse(CurseType.CheaperMerchant))
            {
                swordsToPick = Math.Min(1, SwordsToPick);
                ringsToPick = Math.Min(1, RingsToPick);
                specialRingsToPick = Math.Min(1, SpecialRingsToPick);
                bootsToPick = Math.Min(1, BootsToPick);
                specialFoodToPick = Math.Min(1, SpecialFoodToPick);
            }

            List<string> chosenSwords = PickNFromList(Swords.Keys.ToList(), swordsToPick, random);
            List<string> chosenRings = PickNFromList(Rings.Keys.ToList(), ringsToPick, random);
            List<string> chosenSpecialRings = PickNFromList(SpecialRings.Keys.ToList(), specialRingsToPick, random);
            List<string> chosenBoots = PickNFromList(Boots.Keys.ToList(), bootsToPick, random);
            List<string> chosenSpecialFood = PickNFromList(SpecialFood.Keys.ToList(), specialFoodToPick, random);

            foreach (string swordId in chosenSwords)
                Merchant.AddStock(stock, new MeleeWeapon(swordId), buyPrice: CalculateBuyPrice(Swords[swordId].Item1, Swords[swordId].Item2, priceAdjustment, random), limitedQuantity: Swords[swordId].Item3);

            foreach (string ringId in chosenRings)
                Merchant.AddStock(stock, new Ring(ringId), buyPrice: CalculateBuyPrice(Rings[ringId].Item1, Rings[ringId].Item2, priceAdjustment, random), limitedQuantity: Rings[ringId].Item3);

            foreach (string ringId in chosenSpecialRings)
                Merchant.AddStock(stock, new Ring(ringId), buyPrice: CalculateBuyPrice(SpecialRings[ringId].Item1, SpecialRings[ringId].Item2, priceAdjustment, random), limitedQuantity: SpecialRings[ringId].Item3);

            foreach (string bootId in chosenBoots)
                Merchant.AddStock(stock, new Boots(bootId), buyPrice: CalculateBuyPrice(Boots[bootId].Item1, Boots[bootId].Item2, priceAdjustment, random), limitedQuantity: Boots[bootId].Item3);

            foreach (string specialFoodId in chosenSpecialFood)
                Merchant.AddStock(stock, new SObject(specialFoodId, SpecialFood[specialFoodId].Item3), buyPrice: CalculateBuyPrice(SpecialFood[specialFoodId].Item1, SpecialFood[specialFoodId].Item2, priceAdjustment, random), limitedQuantity: SpecialFood[specialFoodId].Item3);
        }
    }
}

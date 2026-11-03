using Microsoft.Xna.Framework;
using StardewValley.GameData.BigCraftables;
using StardewValley.GameData.Characters;
using StardewModdingAPI;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI.Events;
using StardewRoguelike.ChallengeFloors;
using StardewRoguelike.Extensions;
using StardewRoguelike.Patches;
using StardewRoguelike.UI;
using StardewRoguelike.VirtualProperties;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Minigames;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using xTile.Dimensions;
using xTile.Tiles;

namespace StardewRoguelike
{
    public static class Merchant
    {
        public static readonly List<MerchantFloor> MerchantFloors = PopulateMerchantFloors();

        public static readonly List<Vector2> GardenPotTiles = new()
        {
            new(3, 5),
            new(3, 6),
            new(3, 7),
            new(3, 8),
            new(3, 9)
        };

        public static ShopMenu? CurrentShop { get; set; } = null;

        public static ShopMenu? CurrentSeedShop { get; set; } = null;

        public static HatBoard? CurrentHatBoard { get; set; } = null;

        internal static CurseType? CurseToAdd { get; set; } = null;

        public static string GetMapPath(MineShaft mine)
        {
            string result;
            int level = Roguelike.GetLevelFromMineshaft(mine);
            if (DebugCommands.ForcedFortuneTeller)
                result = "custom-merchant-curses";
            else if (level == 1 || level == Constants.ScalingOrder[0] || level == Constants.ScalingOrder[2])
                result = "custom-merchant";
            else if (level == Constants.ScalingOrder[1])
                result = "custom-merchant-curses";
            else
                result = Roguelike.FloorRng.NextDouble() < 0.5 ? "custom-merchant-curses" : "custom-merchant";

            return result + (Roguelike.HardMode ? "-hard" : "");
        }

        public static bool IsMerchantFloor(MineShaft mine)
        {
            return IsMerchantFloor(Roguelike.GetLevelFromMineshaft(mine));
        }

        public static bool IsMerchantFloor(int level)
        {
            return level != 0 && (level % 6 == 0 || level == 1);
        }

        public static void Initialize(MineShaft mine)
        {
            SpawnMarlon(mine);

            int level = Roguelike.GetLevelFromMineshaft(mine);

            if (level == 1 && !Roguelike.HardMode)
            {
                Vector2 signTile = new(17, 16);
                Sign sign = new(signTile, "38");
                sign.displayItem.Value = new SObject("773", 1);
                sign.displayType.Value = 1;
                mine.Objects.Add(signTile, sign);
            }

            foreach (Vector2 potTile in GardenPotTiles)
            {
                mine.Objects.Add(potTile, CreateGardenPot(mine, potTile));
                Vector2 sprinklerTile = new(potTile.X - 1, potTile.Y);
                var sprinkler = new SObject("599", 1)
                {
                    TileLocation = sprinklerTile
                };
                mine.Objects.Add(sprinklerTile, sprinkler);
            }

            GrowCrops(mine);
        }

        public static void GrowCrop(Crop crop)
        {
            int plantedAgo = crop.get_CropMerchantsPlantedAgo();

            if (plantedAgo == 0)
                throw new Exception("Crop tried to grow but it was just planted!");

            switch (crop.netSeedIndex.Value)
            {
                case "472":  // Parsnips
                    crop.currentPhase.Value = crop.phaseDays.Count - 1;
                    break;
                case "479":  // Melons
                    if (plantedAgo == 1)
                        crop.currentPhase.Value = 2;
                    else
                        crop.currentPhase.Value = crop.phaseDays.Count - 1;
                    break;
                case "490":  // Pumpkins
                    if (plantedAgo == 1)
                        crop.currentPhase.Value = 1;
                    else if (plantedAgo == 2)
                        crop.currentPhase.Value = 3;
                    else
                        crop.currentPhase.Value = crop.phaseDays.Count - 1;
                    break;
                case "486":  // Starfruit
                    if (plantedAgo == 1)
                        crop.currentPhase.Value = 1;
                    else if (plantedAgo == 2)
                        crop.currentPhase.Value = 3;
                    else if (plantedAgo == 3)
                        crop.currentPhase.Value = 4;
                    else
                        crop.currentPhase.Value = crop.phaseDays.Count - 1;
                    break;
                case "347":  // Sweet Gem Berry
                    if (plantedAgo == 1)
                        crop.currentPhase.Value = 1;
                    else if (plantedAgo == 2)
                        crop.currentPhase.Value = 1;
                    else if (plantedAgo == 3)
                        crop.currentPhase.Value = 3;
                    else if (plantedAgo == 4)
                        crop.currentPhase.Value = 4;
                    else if (plantedAgo == 5)
                        crop.currentPhase.Value = 5;
                    else
                        crop.currentPhase.Value = crop.phaseDays.Count - 1;
                    break;
            }
        }

        public static void GrowCrops(MineShaft mine)
        {
            int level = Roguelike.GetLevelFromMineshaft(mine);

            int previousMerchantLevel = level > 6 ? level - 6 : 1;
            MineShaft? previousMerchant = ChallengeFloor.GetMineFromLevel(previousMerchantLevel);
            if (previousMerchant is null)
                return;

            foreach (Vector2 tile in GardenPotTiles)
            {
                IndoorPot currentPot = (IndoorPot)mine.Objects[tile];
                IndoorPot previousPot = (IndoorPot)previousMerchant.Objects[tile];
                Crop previousCrop = previousPot.hoeDirt.Value.crop;
                if (previousCrop is null)
                    continue;

                Crop currentCrop = new(previousCrop.netSeedIndex.Value, (int)tile.X, (int)tile.Y, mine);
                currentCrop.set_CropMerchantsPlantedAgo(previousCrop.get_CropMerchantsPlantedAgo() + 1);

                GrowCrop(currentCrop);
                currentPot.hoeDirt.Value.crop = currentCrop;
            }
        }

        public static void PlayerWarped(object? sender, WarpedEventArgs e)
        {
            if (e.NewLocation is MineShaft mine && IsMerchantFloor(mine))
            {
                Perks.CurrentMenu = new();
                CurseToAdd = Curse.GetRandomUniqueCurse(Roguelike.FloorRng);

                ShopMenu menu;
                if (Perks.HasPerk(Perks.PerkType.Indecisive))
                    menu = new RefreshableShopMenu(GetMerchantStock(random: Roguelike.FloorRng), false, on_purchase: OnPurchase);
                else
                    menu = new(MerchantShopId, GetMerchantStock(random: Roguelike.FloorRng), on_purchase: OnPurchase, playOpenSound: false);
                ApplySellableCategories(menu);
                CurrentShop = menu;
                CurrentSeedShop = new(SeedShopId, GetSeedStock(), playOpenSound: false);
                CurrentHatBoard = new();

                foreach (Vector2 potTile in GardenPotTiles)
                {
                    Vector2 sprinklerTile = new(potTile.X - 1, potTile.Y);
                    mine.Objects[sprinklerTile].ApplySprinklerAnimation();
                }

                if (Perks.HasPerk(Perks.PerkType.Deconstructor))
                {
                    Vector2 deconstructorTile = new(26, 7);
                    mine.Objects[deconstructorTile] = new SObject(deconstructorTile, "265");
                }

                int level = Roguelike.GetLevelFromMineshaft(mine);
                Vector2 dialogueLocation;

                if (level == 1)
                {
                    // Do Marlon introduction
                    dialogueLocation = new Vector2(23, 3) * 64f;
                    dialogueLocation.X += 32;
                    dialogueLocation.Y -= 16;
                    mine.DrawSpeechBubble(dialogueLocation, I18n.Merchant_MarlonIntroduction(), 400);
                }
                else if (level == Constants.ScalingOrder[0])
                {
                    // Do Gil introduction
                    dialogueLocation = new Vector2(18, 3) * 64f;
                    dialogueLocation.X -= 16;
                    dialogueLocation.Y -= 16;
                    mine.DrawSpeechBubble(dialogueLocation, I18n.Merchant_GilIntroduction(), 400);

                }
                else if (level == Constants.ScalingOrder[1])
                {
                    // Do fortune teller introduction
                    dialogueLocation = new Vector2(13, 3) * 64f;
                    dialogueLocation.X -= 16;
                    dialogueLocation.Y -= 16;
                    mine.DrawSpeechBubble(dialogueLocation, I18n.Merchant_FortuneIntroduction(), 400);
                }
                else if (level == Constants.ScalingOrder[2])
                {
                    // Spawn hat board
                    // 1.6 takes the tilesheet's ID rather than its index.
                    string townTilesheetId = mine.map.GetTileSheet("z_Town").Id;

                    mine.setMapTile(11, 5, 2045, "Buildings", townTilesheetId);
                    mine.setMapTile(12, 5, 2046, "Buildings", townTilesheetId);
                    mine.setMapTile(13, 5, 2047, "Buildings", townTilesheetId);
                    mine.setTileProperty(11, 5, "Buildings", "Action", "HatBoard");
                    mine.setTileProperty(12, 5, "Buildings", "Action", "HatBoard");
                    mine.setTileProperty(13, 5, "Buildings", "Action", "HatBoard");
                    mine.setMapTile(11, 4, 2013, "Front", townTilesheetId);
                    mine.setMapTile(12, 4, 2014, "Front", townTilesheetId);
                    mine.setMapTile(13, 4, 2015, "Front", townTilesheetId);
                }

                else if (level == Constants.ScalingOrder[^1])
                {
                    // Spawn Qi when victory
                    SpawnQi(mine);
                    PopulateQiDialogue(mine);
                }
            }
        }

        public static List<string> GetMusicTracks()
        {
            return new() { "Saloon1", "MarlonsTheme" };
        }

        public static void PopulateQiDialogue(GameLocation mine)
        {
            NPC qi = mine.getCharacterFromName("Mister Qi");
            if (qi is null)
                return;

            qi.CurrentDialogue.Clear();

            qi.setNewDialogue(new Dialogue(qi, null, I18n.Merchant_QiVictory()));
        }

        /// <summary>
        /// Puts a villager on a floor, creating them if the world does not already
        /// have one.
        ///
        /// 1.6 only instantiates villagers in loaded locations, so
        /// Game1.getCharacterFromName returns null for anyone whose home location the
        /// run never loads. The roguelike borrows Qi and Marlon purely as set
        /// dressing, so when they are missing they are built straight from
        /// Data/Characters instead.
        /// </summary>
        /// <summary>
        /// Builds a watered garden pot for a floor that is still being generated.
        ///
        /// 1.6's <c>IndoorPot(Vector2)</c> constructor reads
        /// <see cref="Game1.currentLocation"/> to bind itself and its dirt, and
        /// dereferences it unconditionally. During floor generation the warp is still
        /// in progress and that is null, so the constructor throws; even when it
        /// doesn't, the pot would be bound to wherever the player is standing instead
        /// of the floor being built. This does the same setup against the mine.
        /// </summary>
        private static IndoorPot CreateGardenPot(MineShaft mine, Vector2 tile)
        {
            IndoorPot pot = new()
            {
                ItemId = GardenPotItemId,
                Location = mine,
                TileLocation = tile
            };

            pot.bigCraftable.Value = true;
            pot.canBeSetDown.Value = true;

            if (Game1.bigCraftableData.TryGetValue(GardenPotItemId, out BigCraftableData? data))
            {
                pot.Name = data.Name;
                pot.price.Value = data.Price;
                pot.type.Value = "Crafting";
                pot.Category = -9;
                pot.setOutdoors.Value = data.CanBePlacedOutdoors;
                pot.setIndoors.Value = data.CanBePlacedIndoors;
                pot.fragility.Value = data.Fragility;
                pot.isLamp.Value = data.IsLamp;
            }

            pot.ResetParentSheetIndex();
            pot.hoeDirt.Value = new HoeDirt(HoeDirt.watered, mine);
            pot.showNextIndex.Value = true;

            return pot;
        }

        /// <summary>The Garden Pot's big-craftable item ID.</summary>
        private const string GardenPotItemId = "62";

        public static void SpawnVillager(MineShaft mine, string name, Vector2 tile, int facingDirection)
        {
            if (mine.getCharacterFromName(name) is not null)
                return;

            NPC? villager = Game1.getCharacterFromName(name) ?? CreateVillagerFromData(name, mine, tile);
            if (villager is null)
            {
                ModEntry.ModMonitor.Log($"Could not spawn '{name}'; no character data found.", LogLevel.Warn);
                return;
            }

            villager.setTileLocation(tile);
            villager.faceDirection(facingDirection);
            mine.addCharacter(villager);
        }

        private static NPC? CreateVillagerFromData(string name, MineShaft mine, Vector2 tile)
        {
            if (!Game1.characterData.TryGetValue(name, out CharacterData? data))
                return null;

            try
            {
                string textureName = NPC.getTextureNameForCharacter(name);
                return new NPC(
                    new AnimatedSprite($"Characters\\{textureName}", 0, data.Size.X, data.Size.Y),
                    tile * 64f,
                    mine.Name,
                    0,
                    name,
                    data.CanBeRomanced,
                    Game1.content.Load<Texture2D>($"Portraits\\{textureName}")
                );
            }
            catch (Exception ex)
            {
                ModEntry.ModMonitor.Log($"Failed to create '{name}': {ex.Message}", LogLevel.Warn);
                return null;
            }
        }

        public static void SpawnQi(MineShaft mine)
        {
            SpawnVillager(mine, "Mister Qi", new(17, 11), 3);
        }

        public static void SpawnMarlon(MineShaft mine)
        {
            SpawnVillager(mine, "Marlon", new(23, 5), 2);
        }

        public static void DespawnGil(MineShaft mine)
        {
            mine.removeTileProperty(17, 4, "Buildings", "Action");
            mine.removeTileProperty(18, 4, "Buildings", "Action");
            mine.removeTileProperty(17, 5, "Buildings", "Action");
            mine.removeTileProperty(18, 5, "Buildings", "Action");

            mine.removeTile(17, 4, "Front");
            mine.removeTile(18, 4, "Front");
            mine.removeTile(17, 5, "Buildings");
            mine.removeTile(18, 5, "Buildings");
        }

        public static void SpawnBackpack(MineShaft mine)
        {
            mine.setTileProperty(19, 5, "Buildings", "Action", "RoguelikeBackpack");
            mine.setTileProperty(19, 6, "Buildings", "Action", "RoguelikeBackpack");
        }

        public static bool ShouldSpawnBackpack()
        {
            return Game1.player.MaxItems == 12;
        }

        public static bool ShouldSpawnGil(int level)
        {
            int perkCount = Perks.GetActivePerks().Count;
            return (level > 1 && perkCount < Constants.MaximumPerkCount) || DebugCommands.ForcedGil;
        }

        public static void SetupForLocalPlayer(MineShaft mine)
        {
            if (!ShouldSpawnGil(Roguelike.GetLevelFromMineshaft(mine)))
                DespawnGil(mine);

            if (ShouldSpawnBackpack())
                SpawnBackpack(mine);
        }

        public static MerchantFloor GetNextMerchantFloor(MineShaft mine)
        {
            int level = Roguelike.GetLevelFromMineshaft(mine);
            return GetNextMerchantFloor(level);
        }

        public static MerchantFloor GetNextMerchantFloor(int level)
        {
            int index = (level / 6) + 1;
            if (index > MerchantFloors.Count - 1)
                index = MerchantFloors.Count - 1;

            return MerchantFloors[index];
        }

        public static bool AnswerDialogueAction(MineShaft mine, string questionAndAnswer, string[] questionParams)
        {
            if (questionAndAnswer.StartsWith("cursePurchase"))
            {
                int hpNeeded = 15;
                int goldNeeded = 500;

                bool paid = false;

                if (questionAndAnswer == "cursePurchase_YesHP")
                {
                    if (Game1.player.maxHealth > hpNeeded)
                    {
                        Roguelike.TrueMaxHP -= hpNeeded;
                        paid = true;
                    }
                    else
                        Game1.drawObjectDialogue(I18n.Merchant_NotEnoughHP());
                }
                else if (questionAndAnswer == "cursePurchase_YesGold")
                {
                    if (Game1.player.Money >= goldNeeded)
                    {
                        Game1.player.Money -= goldNeeded;
                        paid = true;
                    }
                    else
                        Game1.drawObjectDialogue(I18n.Merchant_NotEnoughGold());
                }

                if (paid && CurseToAdd is not null)
                {
                    Curse.AddCurse(CurseToAdd.Value);
                    Game1.playSound("debuffSpell");
                    mine.set_MineShaftUsedFortune(true);
                }
            }
            else if (questionAndAnswer == "roguelikeBackpackPurchase_Yes")
            {
                int goldNeeded = 2000;

                if (Game1.player.Money < goldNeeded)
                    Game1.drawObjectDialogue(I18n.Merchant_NotEnoughGold());
                else if (Game1.player.MaxItems > 12)
                    Game1.drawObjectDialogue(I18n.Merchant_AlreadyHaveBackpack());
                else
                {
                    Game1.player.Money -= goldNeeded;
                    Game1.player.maxItems.Value += 12;
                    for (int i = 0; i < Game1.player.MaxItems; i++)
                    {
                        if (Game1.player.Items.Count <= i)
                            Game1.player.Items.Add(null);
                    }

                    Game1.player.holdUpItemThenMessage(new SpecialItem(99, Game1.content.LoadString("Strings\\StringsFromCSFiles:GameLocation.cs.8708")));
                    mine.removeTileProperty(19, 5, "Buildings", "Action");
                    mine.removeTileProperty(19, 6, "Buildings", "Action");
                }
            }

            return false;
        }

        public static bool PerformAction(MineShaft mine, string action, Farmer who, Location tileLocation)
        {
            if (action == "Curses" && !mine.get_MineShaftUsedFortune())
            {
                if (Curse.HasAllCurses() || !CurseToAdd.HasValue)
                {
                    Game1.drawObjectDialogue(I18n.Merchant_HasAllCurses());
                    return true;
                }

                int hpNeeded = 15;
                int goldNeeded = 500;

                var responses = new Response[3]
                {
                    new Response("YesGold", I18n.Merchant_YesGold(goldNeeded: goldNeeded)),
                    new Response("YesHP", I18n.Merchant_YesHP(hpNeeded: hpNeeded)),
                    new Response("No", Game1.content.LoadString("Strings\\Lexicon:QuestionDialogue_No"))
                };

                mine.createQuestionDialogue(I18n.Merchant_DoCurse(), responses, "cursePurchase");
                return true;
            }
            else if (action == "RoguelikeBackpack")
            {
                var responses = mine.createYesNoResponses();
                mine.createQuestionDialogue(I18n.Merchant_DoBackpackUpgrade(), responses, "roguelikeBackpackPurchase");
                return true;
            }
            else if (action == "Arcade_Minecart")
            {
                Game1.currentMinigame = new MineCart(0, 2);
                return true;
            }
            else if (action == "HatBoard")
            {
                Game1.activeClickableMenu = CurrentHatBoard;
                Game1.playSound("bigSelect");

                return true;
            }

            return false;
        }

        public static void Draw(MineShaft mine, SpriteBatch b)
        {
            if (ShouldSpawnBackpack())
                b.Draw(Game1.mouseCursors, Game1.GlobalToLocal(new Vector2(1224, 320)), new Microsoft.Xna.Framework.Rectangle(255, 1436, 12, 14), Color.White, 0f, Vector2.Zero, 4f, SpriteEffects.None, 0.01232f);
        }

        /// <summary>
        /// The shop IDs the roguelike's own menus are registered under. 1.6 requires
        /// every <see cref="ShopMenu"/> to carry one.
        /// </summary>
        public const string MerchantShopId = $"{ModEntry.ModId}_Merchant";

        public const string SeedShopId = $"{ModEntry.ModId}_SeedShop";

        /// <summary>
        /// Lets the player sell minerals, gems and metals at the merchant.
        ///
        /// 1.5.6 got this from passing a "Blacksmith" store context; in 1.6 the
        /// sellable categories come from <c>Data/Shops</c>, so a mod-owned shop has
        /// to set them itself.
        /// </summary>
        private static void ApplySellableCategories(ShopMenu menu)
        {
            menu.categoriesToSellHere.AddRange(new[]
            {
                SObject.mineralsCategory,
                SObject.GemCategory,
                SObject.metalResources
            });
        }

        /// <summary>
        /// Adds an item to a shop's stock at a fixed price, with unlimited stock
        /// unless a quantity is given.
        ///
        /// Replaces <c>Utility.AddStock</c>, which 1.6 removed along with the rest of
        /// the pre-<c>Data/Shops</c> stock helpers. The roguelike already overrode its
        /// behaviour with a Harmony patch, so this keeps that logic directly.
        /// </summary>
        public static void AddStock(Dictionary<ISalable, ItemStockInformation> stock, Item obj, int buyPrice = -1, int limitedQuantity = -1)
        {
            int price = buyPrice == -1 ? obj.salePrice() : buyPrice;

            int stackCount = ShopMenu.infiniteStock;
            if (obj is SObject sobj && sobj.IsRecipe)
                stackCount = 1;
            else if (limitedQuantity != -1)
                stackCount = limitedQuantity;

            stock[obj] = new ItemStockInformation(price, stackCount);
        }

        /// <summary>
        /// Tracks iridium milk purchases, which are capped per run.
        /// </summary>
        public static bool OnPurchase(ISalable item, Farmer who, int numberToBuy, ItemStockInformation stock)
        {
            if (item is SObject boughtItem && boughtItem.QualifiedItemId == "(O)803")
                Roguelike.MilksBought += numberToBuy;

            return false;
        }

        /// <summary>
        /// Opens one of the roguelike's menus for a "Buy &lt;name&gt;" map action.
        ///
        /// 1.5.6 hooked <c>GameLocation.openShopMenu</c>, which 1.6 removed in favour
        /// of data-driven shops, so the mod's own shop names are dispatched here
        /// instead of through a Harmony patch.
        /// </summary>
        public static bool TryOpenShop(GameLocation location, string which)
        {
            if (location is not MineShaft mine)
                return false;

            switch (which)
            {
                case "Roguelike":
                    if (CurrentShop is null)
                        return false;

                    if (CurrentShop is not RefreshableShopMenu && Perks.HasPerk(Perks.PerkType.Indecisive))
                    {
                        CurrentShop = new RefreshableShopMenu(CurrentShop.itemPriceAndStock, false, on_purchase: OnPurchase);
                        ApplySellableCategories(CurrentShop);
                    }

                    foreach (ISalable buybackItem in CurrentShop.buyBackItems)
                        CurrentShop.itemPriceAndStock.Remove(buybackItem);

                    CurrentShop.forSale = CurrentShop.itemPriceAndStock.Keys.ToList();

                    CurrentShop.currentItemIndex = 0;

                    // Still non-public in 1.6; without it the scroll thumb keeps its
                    // old position while the list resets to the top.
                    ModEntry.ReflectionHelper
                        .GetMethod(CurrentShop, "setScrollBarToCurrentIndex")
                        .Invoke();

                    CurrentShop.buyBackItems.Clear();
                    CurrentShop.buyBackItemsToResellTomorrow.Clear();
                    Game1.activeClickableMenu = CurrentShop;
                    return true;

                case "RoguelikeDiscounted":
                    if (!ChallengeFloor.IsChallengeFloor(mine))
                        return false;

                    PickAPath pickAPath = (PickAPath)mine.get_MineShaftChallengeFloor().Value;
                    Game1.activeClickableMenu = pickAPath.CurrentShop;
                    return true;

                case "Perks":
                    Perks.CurrentMenu.isActive = true;
                    Perks.CurrentMenu.informationUp = true;
                    Game1.activeClickableMenu = Perks.CurrentMenu;
                    return true;

                case "Seeds":
                    Game1.activeClickableMenu = CurrentSeedShop;
                    return true;

                default:
                    return false;
            }
        }

        public static Dictionary<ISalable, ItemStockInformation> GetSeedStock()
        {
            Dictionary<ISalable, ItemStockInformation> stock = new();

            AddStock(stock, new SObject("472", 5), 50, 5);   // Parsnip Seeds
            AddStock(stock, new SObject("479", 5), 100, 5);  // Melon Seeds
            AddStock(stock, new SObject("490", 5), 150, 5);  // Pumpkin Seeds
            AddStock(stock, new SObject("486", 5), 200, 5);  // Starfruit Seeds
            AddStock(stock, new SObject("347", 5), 250, 5);  // Rare Seeds

            return stock;
        }

        public static Dictionary<ISalable, ItemStockInformation> GetMerchantStock(float priceAdjustment = 1f, Random? random = null)
        {
            random ??= Game1.random;

            Dictionary<ISalable, ItemStockInformation> stock = new();

            if (Perks.HasPerk(Perks.PerkType.Discount))
                priceAdjustment *= 0.9f;

            if (Roguelike.CurrentLevel < Constants.ScalingOrder[0])         // Floor 1 Shop
                MerchantFloors[0].AddToStock(stock, priceAdjustment, random);
            else if (Roguelike.CurrentLevel < Constants.ScalingOrder[1])    // Floor 10 Shop
                MerchantFloors[1].AddToStock(stock, priceAdjustment, random);
            else if (Roguelike.CurrentLevel < Constants.ScalingOrder[2])    // Floor 20 Shop
                MerchantFloors[2].AddToStock(stock, priceAdjustment, random);
            else if (Roguelike.CurrentLevel < Constants.ScalingOrder[3])    // Floor 30 Shop
                MerchantFloors[3].AddToStock(stock, priceAdjustment, random);
            else if (Roguelike.CurrentLevel < Constants.ScalingOrder[4])    // Floor 40 Shop
                MerchantFloors[4].AddToStock(stock, priceAdjustment, random);
            else if (Roguelike.CurrentLevel < Constants.ScalingOrder[5])    // Floor 50 Shop
                MerchantFloors[5].AddToStock(stock, priceAdjustment, random);
            else if (Roguelike.CurrentLevel < Constants.ScalingOrder[6])    // Floor 60 Shop
                MerchantFloors[6].AddToStock(stock, priceAdjustment, random);
            else if (Roguelike.CurrentLevel < Constants.ScalingOrder[7])    // Floor 70 Shop
                MerchantFloors[7].AddToStock(stock, priceAdjustment, random);
            else                                                            // Floor 80+ Shop
                MerchantFloors[8].AddToStock(stock, priceAdjustment, random);

            int foodQuantity = Curse.HasCurse(CurseType.CheaperMerchant) ? 1 : 3;
            int foodPriceOffset = Curse.HasCurse(CurseType.CheaperMerchant) ? -100 : 0;

            AddStock(stock, new SObject("194", foodQuantity), buyPrice: (int)((200 + foodPriceOffset) * priceAdjustment), limitedQuantity: foodQuantity);  // Fried Egg
            AddStock(stock, new SObject("196", foodQuantity), buyPrice: (int)((300 + foodPriceOffset) * priceAdjustment), limitedQuantity: foodQuantity);  // Salad
            AddStock(stock, new SObject("773", foodQuantity), buyPrice: (int)((500 + foodPriceOffset) * priceAdjustment), limitedQuantity: foodQuantity);  // Life Elixir

            int totalMilksInGame = (Roguelike.MaxHP - Roguelike.StartingHP) / 25;
            int milkQuantity = totalMilksInGame - Roguelike.MilksBought;
            if (milkQuantity > 0)
                AddStock(stock, new SObject("803", milkQuantity), buyPrice: (int)(500 * priceAdjustment), limitedQuantity: milkQuantity);  // Iridium Milk

            return stock;
        }

        // Should only be called once
        public static List<MerchantFloor> PopulateMerchantFloors()
        {
            List<MerchantFloor> result = new();

            int currentIndex = 0;
            while (true)
            {
                MerchantFloor floor;
                if (currentIndex == 0)                          // Floor 1 Shop
                {
                    floor = new(3, 0, 0, 0, 0);

                    floor.AddSword("12", 50, 50, 1);              // Wooden Blade
                    floor.AddSword("11", 50, 50, 1);              // Steel Smallsword
                    floor.AddSword("21", 50, 50, 1);              // Crystal Dagger
                }
                else if (currentIndex == 1)                     // Floor 10 Shop
                {
                    floor = new(3, 2, 0, 1, 0);

                    floor.AddSword("43", 400, 525, 1);            // Pirate's Sword
                    floor.AddSword("1", 400, 525, 1);             // Silver Saber
                    floor.AddSword("15", 650, 800, 1);            // Forest Sword
                    floor.AddSword("6", 700, 750, 1);             // Iron Edge
                    floor.AddSword("19", 325, 450, 1);            // Shadow Dagger
                    floor.AddSword("51", 325, 500, 1);            // Broken Trident

                    floor.AddRing("522", 700, 800, 1);            // Vampire Ring
                    floor.AddRing("532", 500, 600, 1);            // Jade Ring
                    floor.AddRing("531", 500, 600, 1);            // Aquamarine Ring

                    floor.AddBoots("515", 500, 600, 1);           // Cowboy Boots
                    floor.AddBoots("513", 600, 700, 1);           // Genie Shoes
                    floor.AddBoots("508", 500, 600, 1);           // Combat Boots
                }
                else if (currentIndex == 2)                     // Floor 20 Shop
                {
                    floor = new(3, 2, 1, 2, 0);

                    floor.AddSword("5", 700, 850, 1);             // Bone Sword
                    floor.AddSword("14", 700, 850, 1);            // Neptune's Glaive
                    floor.AddSword("8", 825, 1025, 1);             // Obsidian Edge
                    floor.AddSword("60", 825, 1025, 1);            // Ossified Blade
                    floor.AddSword("45", 600, 775, 1);            // Wicked Kris
                    floor.AddSword("24", 750, 900, 1);            // Wood Club

                    floor.AddRing("532", 500, 600, 1);            // Jade Ring
                    floor.AddRing("531", 500, 600, 1);            // Aquamarine Ring
                    floor.AddRing("534", 700, 800, 1);            // Ruby Ring
                    floor.AddRing("533", 500, 600, 1);            // Emerald Ring

                    floor.AddSpecialRing("522", 700, 800, 1);     // Vampire Ring
                    floor.AddSpecialRing("523", 600, 700, 1);     // Savage Ring
                    floor.AddSpecialRing("861", 600, 700, 1);     // Protection Ring

                    floor.AddBoots("513", 600, 700, 1);           // Genie Shoes
                    floor.AddBoots("511", 600, 700, 1);           // Dark Boots
                    floor.AddBoots("512", 600, 700, 1);           // Fire Walker Boots
                    floor.AddBoots("878", 800, 900, 1);           // Crystal Shoes
                }
                else if (currentIndex == 3)                     // Floor 30 Shop
                {
                    floor = new(3, 2, 2, 2, 0);

                    floor.AddSword("48", 800, 950, 1);            // Yeti Tooth
                    floor.AddSword("50", 825, 975, 1);            // Steel Falchion
                    floor.AddSword("2", 1000, 1225, 1);            // Dark Sword
                    floor.AddSword("9", 950, 1225, 1);            // Lava Katana
                    floor.AddSword("23", 600, 750, 1);            // Galaxy Dagger
                    floor.AddSword("56", 700, 800, 1);            // Dwarf Dagger
                    floor.AddSword("27", 825, 1000, 1);            // Wood Mallet

                    floor.AddRing("532", 500, 600, 1);            // Jade Ring
                    floor.AddRing("531", 500, 600, 1);            // Aquamarine Ring
                    floor.AddRing("534", 700, 800, 1);            // Ruby Ring
                    floor.AddRing("533", 500, 600, 1);            // Emerald Ring

                    floor.AddSpecialRing("522", 700, 800, 1);     // Vampire Ring
                    floor.AddSpecialRing("523", 600, 700, 1);     // Savage Ring
                    floor.AddSpecialRing("861", 600, 700, 1);     // Protection Ring
                    floor.AddSpecialRing("811", 600, 700, 1);     // Napalm Ring
                    floor.AddSpecialRing("810", 600, 700, 1);     // Crabshell Ring
                    floor.AddSpecialRing("839", 400, 500, 1);     // Thorns Ring
                    floor.AddSpecialRing("521", 600, 700, 1);     // Warrior Ring

                    floor.AddBoots("514", 600, 700, 1);           // Space Boots
                    floor.AddBoots("855", 600, 700, 1);           // Dragon Scale Boots
                    floor.AddBoots("878", 600, 700, 1);           // Crystal Shoes
                }
                else if (currentIndex == 4)                     // Floor 40 Shop
                {
                    floor = new(3, 2, 2, 2, 0);

                    floor.AddSword("2", 800, 1025, 1);             // Dark Sword
                    floor.AddSword("9", 750, 1025, 1);             // Lava Katana
                    floor.AddSword("4", 1000, 1200, 1);            // Galaxy Sword
                    floor.AddSword("54", 1000, 1200, 1);           // Dwarf Sword
                    floor.AddSword("59", 850, 1075, 1);            // Dragontooth Shiv
                    floor.AddSword("61", 925, 1075, 1);            // Iridium Needle
                    floor.AddSword("46", 825, 1100, 1);           // Kudgel

                    floor.AddRing("532", 500, 600, 1);            // Jade Ring
                    floor.AddRing("531", 500, 600, 1);            // Aquamarine Ring
                    floor.AddRing("534", 700, 800, 1);            // Ruby Ring
                    floor.AddRing("533", 500, 600, 1);            // Emerald Ring

                    floor.AddSpecialRing("523", 600, 700, 1);     // Savage Ring
                    floor.AddSpecialRing("861", 600, 700, 1);     // Protection Ring
                    floor.AddSpecialRing("811", 600, 700, 1);     // Napalm Ring
                    floor.AddSpecialRing("810", 600, 700, 1);     // Crabshell Ring
                    floor.AddSpecialRing("863", 600, 700, 1);     // Phoenix Ring
                    floor.AddSpecialRing("524", 800, 900, 1);     // Ring of Yoba

                    floor.AddBoots("514", 600, 700, 1);           // Space Boots
                    floor.AddBoots("855", 600, 700, 1);           // Dragon Scale Boots
                    floor.AddBoots("878", 600, 700, 1);           // Crystal Shoes
                    floor.AddBoots("854", 800, 1100, 1);          // Mermaid Boots
                    floor.AddBoots("853", 800, 1100, 1);          // Cinderclown Shoes
                }
                else if (currentIndex == 5)                     // Floor 50 Shop
                {
                    floor = new(3, 2, 2, 2, 1);

                    floor.AddSword("2", 800, 1025, 1);             // Dark Sword
                    floor.AddSword("4", 1100, 1225, 1);           // Galaxy Sword
                    floor.AddSword("54", 1150, 1275, 1);          // Dwarf Sword
                    floor.AddSword("59", 1000, 1175, 1);           // Dragontooth Shiv
                    floor.AddSword("28", 1275, 1350, 1);          // Slammer

                    floor.AddRing("532", 500, 600, 1);            // Jade Ring
                    floor.AddRing("531", 500, 600, 1);            // Aquamarine Ring
                    floor.AddRing("534", 700, 800, 1);            // Ruby Ring
                    floor.AddRing("533", 500, 600, 1);            // Emerald Ring

                    floor.AddSpecialRing("523", 600, 700, 1);     // Savage Ring
                    floor.AddSpecialRing("861", 600, 700, 1);     // Protection Ring
                    floor.AddSpecialRing("811", 600, 700, 1);     // Napalm Ring
                    floor.AddSpecialRing("810", 600, 700, 1);     // Crabshell Ring
                    floor.AddSpecialRing("863", 600, 700, 1);     // Phoenix Ring
                    floor.AddSpecialRing("524", 800, 900, 1);     // Ring of Yoba
                    floor.AddSpecialRing("839", 600, 700, 1);     // Thorns Ring
                    floor.AddSpecialRing("521", 600, 700, 1);     // Warrior Ring
                    floor.AddSpecialRing("887", 600, 700, 1);     // Immunity Band

                    floor.AddBoots("514", 600, 700, 1);           // Space Boots
                    floor.AddBoots("855", 600, 700, 1);           // Dragon Scale Boots
                    floor.AddBoots("878", 600, 700, 1);           // Crystal Shoes
                    floor.AddBoots("854", 800, 1100, 1);          // Mermaid Boots
                    floor.AddBoots("853", 800, 1100, 1);          // Cinderclown Shoes

                    floor.AddSpecialFood("244", 1850, 2100, 1);   // Root Platter
                    floor.AddSpecialFood("253", 2100, 2350, 1);   // Triple Shot Espresso
                    floor.AddSpecialFood("231", 2100, 2600, 1);   // Eggplant Parmesan
                    floor.AddSpecialFood("204", 1850, 2100, 1);   // Lucky Lunch
                }
                else if (currentIndex == 6)                     // Floor 60 Shop
                {
                    floor = new(3, 2, 2, 2, 1);

                    floor.AddSword("4", 1100, 1225, 1);           // Galaxy Sword
                    floor.AddSword("54", 1150, 1275, 1);          // Dwarf Sword
                    floor.AddSword("57", 1350, 1475, 1);          // Dragontooth Cutlass
                    floor.AddSword("59", 1000, 1175, 1);           // Dragontooth Shiv
                    floor.AddSword("61", 1100, 1225, 1);          // Iridium Needle
                    floor.AddSword("28", 1275, 1350, 1);          // Slammer

                    floor.AddRing("532", 500, 600, 1);            // Jade Ring
                    floor.AddRing("531", 500, 600, 1);            // Aquamarine Ring
                    floor.AddRing("534", 700, 800, 1);            // Ruby Ring
                    floor.AddRing("533", 500, 700, 1);            // Emerald Ring

                    floor.AddSpecialRing("523", 600, 700, 1);     // Savage Ring
                    floor.AddSpecialRing("861", 600, 700, 1);     // Protection Ring
                    floor.AddSpecialRing("811", 600, 700, 1);     // Napalm Ring
                    floor.AddSpecialRing("810", 600, 700, 1);     // Crabshell Ring
                    floor.AddSpecialRing("863", 600, 700, 1);     // Phoenix Ring
                    floor.AddSpecialRing("524", 800, 900, 1);     // Ring of Yoba
                    floor.AddSpecialRing("839", 600, 700, 1);     // Thorns Ring
                    floor.AddSpecialRing("521", 600, 700, 1);     // Warrior Ring
                    floor.AddSpecialRing("887", 600, 700, 1);     // Immunity Band

                    floor.AddBoots("514", 600, 700, 1);           // Space Boots
                    floor.AddBoots("855", 600, 700, 1);           // Dragon Scale Boots
                    floor.AddBoots("878", 600, 700, 1);           // Crystal Shoes
                    floor.AddBoots("854", 800, 1100, 1);          // Mermaid Boots
                    floor.AddBoots("853", 800, 1100, 1);          // Cinderclown Shoes

                    floor.AddSpecialFood("244", 1850, 2100, 1);   // Root Platter
                    floor.AddSpecialFood("253", 2100, 2350, 1);   // Triple Shot Espresso
                    floor.AddSpecialFood("231", 2100, 2600, 1);   // Eggplant Parmesan
                    floor.AddSpecialFood("204", 1850, 2100, 1);   // Lucky Lunch
                }
                else if (currentIndex == 7)                     // Floor 70 Shop
                {
                    floor = new(3, 2, 2, 2, 1);

                    floor.AddSword("4", 1100, 1225, 1);           // Galaxy Sword
                    floor.AddSword("54", 1150, 1275, 1);          // Dwarf Sword
                    floor.AddSword("57", 1350, 1475, 1);          // Dragontooth Cutlass
                    floor.AddSword("59", 1000, 1175, 1);           // Dragontooth Shiv
                    floor.AddSword("61", 1100, 1225, 1);          // Iridium Needle
                    floor.AddSword("29", 1975, 2100, 1);          // Galaxy Hammer

                    floor.AddRing("532", 500, 600, 1);            // Jade Ring
                    floor.AddRing("531", 500, 600, 1);            // Aquamarine Ring
                    floor.AddRing("534", 700, 800, 1);            // Ruby Ring
                    floor.AddRing("533", 500, 600, 1);            // Emerald Ring

                    floor.AddSpecialRing("523", 600, 700, 1);     // Savage Ring
                    floor.AddSpecialRing("861", 600, 700, 1);     // Protection Ring
                    floor.AddSpecialRing("811", 600, 700, 1);     // Napalm Ring
                    floor.AddSpecialRing("810", 600, 700, 1);     // Crabshell Ring
                    floor.AddSpecialRing("863", 600, 700, 1);     // Phoenix Ring
                    floor.AddSpecialRing("524", 800, 900, 1);     // Ring of Yoba
                    floor.AddSpecialRing("839", 600, 700, 1);     // Thorns Ring
                    floor.AddSpecialRing("521", 600, 700, 1);     // Warrior Ring
                    floor.AddSpecialRing("887", 600, 700, 1);     // Immunity Band

                    floor.AddBoots("514", 600, 700, 1);           // Space Boots
                    floor.AddBoots("855", 600, 700, 1);           // Dragon Scale Boots
                    floor.AddBoots("878", 600, 700, 1);           // Crystal Shoes
                    floor.AddBoots("854", 800, 1100, 1);          // Mermaid Boots
                    floor.AddBoots("853", 800, 1100, 1);          // Cinderclown Shoes

                    floor.AddSpecialFood("244", 1850, 2100, 1);   // Root Platter
                    floor.AddSpecialFood("253", 2100, 2350, 1);   // Triple Shot Espresso
                    floor.AddSpecialFood("231", 2100, 2600, 1);   // Eggplant Parmesan
                    floor.AddSpecialFood("204", 1850, 2100, 1);   // Lucky Lunch
                }
                else if (currentIndex == 8)                     // Floor 80 Shop
                {
                    floor = new(3, 2, 2, 2, 1);

                    floor.AddSword("4", 1100, 1225, 1);           // Galaxy Sword
                    floor.AddSword("23", 1150, 1275, 1);          // Galaxy Dagger
                    floor.AddSword("57", 1350, 1475, 1);          // Dragontooth Cutlass
                    floor.AddSword("59", 1000, 1175, 1);           // Dragontooth Shiv
                    floor.AddSword("61", 1100, 1225, 1);          // Iridium Needle
                    floor.AddSword("29", 1975, 2100, 1);          // Galaxy Hammer
                    floor.AddSword("58", 2050, 2200, 1);          // Dragontooth Club

                    floor.AddRing("532", 500, 600, 1);            // Jade Ring
                    floor.AddRing("531", 500, 600, 1);            // Aquamarine Ring
                    floor.AddRing("534", 700, 800, 1);            // Ruby Ring
                    floor.AddRing("533", 500, 600, 1);            // Emerald Ring

                    floor.AddSpecialRing("523", 600, 700, 1);     // Savage Ring
                    floor.AddSpecialRing("861", 600, 700, 1);     // Protection Ring
                    floor.AddSpecialRing("811", 600, 700, 1);     // Napalm Ring
                    floor.AddSpecialRing("810", 600, 700, 1);     // Crabshell Ring
                    floor.AddSpecialRing("863", 600, 700, 1);     // Phoenix Ring
                    floor.AddSpecialRing("524", 800, 900, 1);     // Ring of Yoba
                    floor.AddSpecialRing("839", 600, 700, 1);     // Thorns Ring
                    floor.AddSpecialRing("521", 600, 700, 1);     // Warrior Ring
                    floor.AddSpecialRing("887", 600, 700, 1);     // Immunity Band

                    floor.AddBoots("514", 600, 700, 1);           // Space Boots
                    floor.AddBoots("855", 600, 700, 1);           // Dragon Scale Boots
                    floor.AddBoots("878", 600, 700, 1);           // Crystal Shoes
                    floor.AddBoots("854", 800, 1100, 1);          // Mermaid Boots
                    floor.AddBoots("853", 800, 1100, 1);          // Cinderclown Shoes

                    floor.AddSpecialFood("244", 1850, 2100, 1);   // Root Platter
                    floor.AddSpecialFood("253", 2100, 2350, 1);   // Triple Shot Espresso
                    floor.AddSpecialFood("231", 2100, 2600, 1);   // Eggplant Parmesan
                    floor.AddSpecialFood("204", 1850, 2100, 1);   // Lucky Lunch
                }
                else
                    break;

                result.Add(floor);
                currentIndex++;
            }

            return result;
        }
    }
}

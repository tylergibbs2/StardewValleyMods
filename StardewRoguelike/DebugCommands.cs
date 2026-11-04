using System;
using StardewRoguelike.VirtualProperties;
using System.Text;
using StardewModdingAPI;
using StardewRoguelike.UI;
using StardewRoguelike.Mode;
using HarmonyLib;
using StardewValley;
using StardewValley.Locations;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Network;
using StardewRoguelike.Bosses;
using System.Text.Json;
using StardewValley.Menus;
using StardewModdingAPI.Events;
using StardewRoguelike.Extensions;
using StardewValley.TerrainFeatures;
using System.Linq;
using System.IO;
using StardewValley.ItemTypeDefinitions;
using System.Collections.Generic;
using StardewValley.Tools;
using StardewValley.Objects;

namespace StardewRoguelike
{
    internal class DebugCommands
    {
        public static float ForcedStoneChance = 0.0f;

        public static float ForcedGemChance = 0.0f;

        public static float ForcedMonsterChance = 0.0f;

        public static float ForcedDarkChance = 0.0f;

        public static int ForcedBossIndex = -1;

        public static int ForcedChallengeIndex = -1;

        public static bool ForcedForgeFloor = false;

        public static bool ForcedChestFloor = false;

        public static float ForcedDifficulty = 0f;

        public static bool ForcedFortuneTeller = false;

        public static bool ForcedGil = false;

        public static void Parse(string command, string[] args)
        {
            if (args.Length == 0)
            {
                PrintHelp();
                return;
            }

            switch (args[0])
            {
                // Audits every item ID the shops can offer, not just the ones a
                // given seed happens to pick. 1.6 resolves IDs through ItemRegistry,
                // which hands back an Error Item for a stale ID instead of throwing,
                // so a broken shop looks like a working one until you read the names.
                case "shops":
                    {
                        int total = 0, bad = 0;

                        void Check(string floor, string kind, string id, Func<Item> make)
                        {
                            total++;
                            Item item;
                            try
                            {
                                item = make();
                            }
                            catch (Exception ex)
                            {
                                bad++;
                                ModEntry.ModMonitor.Log($"  [{floor}] {kind} {id}: THREW {ex.GetType().Name}: {ex.Message}", LogLevel.Error);
                                return;
                            }

                            ParsedItemData? data = ItemRegistry.GetData(item.QualifiedItemId);
                            if (data is null)
                            {
                                bad++;
                                ModEntry.ModMonitor.Log($"  [{floor}] {kind} {id}: UNRESOLVED (qid={item.QualifiedItemId})", LogLevel.Error);
                            }
                            else if (data.IsErrorItem || item.Name == "Error Item")
                            {
                                bad++;
                                ModEntry.ModMonitor.Log($"  [{floor}] {kind} {id}: ERROR ITEM (qid={item.QualifiedItemId})", LogLevel.Error);
                            }
                        }

                        for (int i = 0; i < Merchant.MerchantFloors.Count; i++)
                        {
                            MerchantFloor f = Merchant.MerchantFloors[i];
                            string name = $"floor{i}";
                            foreach (string id in f.Swords.Keys)
                                Check(name, "sword", id, () => new MeleeWeapon(id));
                            foreach (string id in f.Rings.Keys)
                                Check(name, "ring", id, () => new Ring(id));
                            foreach (string id in f.SpecialRings.Keys)
                                Check(name, "specialring", id, () => new Ring(id));
                            foreach (string id in f.Boots.Keys)
                                Check(name, "boots", id, () => new Boots(id));
                            foreach (string id in f.SpecialFood.Keys)
                                Check(name, "food", id, () => new SObject(id, 1));
                        }

                        foreach (string id in new[] { "194", "196", "773", "803", "599" })
                            Check("fixed", "object", id, () => new SObject(id, 1));

                        foreach (ISalable s in Merchant.GetSeedStock().Keys)
                            Check("seedshop", "seed", s.QualifiedItemId, () => (Item)s);

                        ModEntry.ModMonitor.Log($"shop id audit: {total} ids checked, {bad} broken", bad > 0 ? LogLevel.Error : LogLevel.Info);

                        // Now the menus themselves, with their real stock.
                        var built = Merchant.GetMerchantStock(random: new Random(1));
                        ModEntry.ModMonitor.Log($"merchant stock for level {Roguelike.CurrentLevel}: {built.Count} items", LogLevel.Info);
                        foreach (var (item, info) in built)
                            ModEntry.ModMonitor.Log($"  {item.DisplayName} (qid={item.QualifiedItemId}) price={info.Price} stock={info.Stock}", LogLevel.Info);

                        var seeds = Merchant.GetSeedStock();
                        ModEntry.ModMonitor.Log($"seed stock: {seeds.Count} items", LogLevel.Info);
                        foreach (var (item, info) in seeds)
                            ModEntry.ModMonitor.Log($"  {item.DisplayName} (qid={item.QualifiedItemId}) price={info.Price} stock={info.Stock}", LogLevel.Info);
                    }
                    break;

                // Opens one of the roguelike's menus for real, so it draws and any
                // exception lands in the log the way it would in play.
                case "openshop":
                    {
                        string which = args.Length > 1 ? args[1] : "Roguelike";
                        bool opened = Merchant.TryOpenShop(Game1.currentLocation, which);
                        IClickableMenu? menu = Game1.activeClickableMenu;
                        ModEntry.ModMonitor.Log($"openshop {which}: handled={opened} menu={menu?.GetType().Name ?? "none"}", LogLevel.Info);

                        if (menu is ShopMenu shop)
                        {
                            ModEntry.ModMonitor.Log(
                                $"  shopId={shop.ShopId} forSale={shop.forSale.Count} stock={shop.itemPriceAndStock.Count} " +
                                $"sellableCategories={string.Join(",", shop.categoriesToSellHere)}",
                                LogLevel.Info);
                        }
                    }
                    break;

                case "closeshop":
                    Game1.activeClickableMenu = null;
                    ModEntry.ModMonitor.Log("closed menu", LogLevel.Info);
                    break;

                case "help":
                    PrintHelp();
                    break;
                case "perk":
                    if (args.Length == 1)
                    {
                        var values = Enum.GetValues<Perks.PerkType>();
                        for (int i = 0; i < values.Length; i++)
                            ModEntry.ModMonitor.Log($"{i} = {values[i]}", LogLevel.Info);
                    }
                    else
                    {
                        Perks.PerkType perk = (Perks.PerkType)int.Parse(args[1]);
                        Perks.AddPerk(perk);
                    }
                    break;
                case "perkpick":
                    Game1.activeClickableMenu = new PerkMenu();
                    break;
                case "perks":
                    for (int i = 0; i < Enum.GetValues<Perks.PerkType>().Length; i++)
                        Perks.AddPerk(Perks.GetRandomUniquePerk()!.Value);
                    break;
                case "clearperks":
                    Perks.RemoveAllPerks();
                    break;
                case "curse":
                    if (args.Length == 1)
                    {
                        var values = Enum.GetValues<CurseType>();
                        for (int i = 0; i < values.Length; i++)
                            ModEntry.ModMonitor.Log($"{i} = {values[i]}", LogLevel.Info);
                    }
                    else
                    {
                        var values = Enum.GetValues<CurseType>();
                        CurseType curse = values[int.Parse(args[1])];
                        Curse.AddCurse(curse);
                    }
                    break;
                case "curses":
                    var allCurses = Enum.GetValues<CurseType>();
                    for (int i = 0; i < allCurses.Length; i++)
                        Curse.AddCurse(allCurses[i]);
                    break;
                case "clearcurses":
                    Curse.RemoveAllCurses();
                    break;
                case "ladder":
                    ForceLadder(args);
                    break;
                case "reset":
                    ModEntry.MultiplayerHelper.SendMessage(
                        "GameOver",
                        "GameOver"
                    );
                    Roguelike.GameOver();
                    break;
                case "spectate":
                    SpectatorMode.EnterSpectatorMode();
                    break;
                case "unspectate":
                    SpectatorMode.ExitSpectatorMode();
                    break;
                case "seed":
                    if (args.Length == 1)
                        ModEntry.ModMonitor.Log($"Floor generation seed: {Roguelike.FloorRngSeed}", LogLevel.Info);
                    else
                    {
                        Roguelike.FloorRngSeed = int.Parse(args[1]);
                        Roguelike.FloorRng = new(Roguelike.FloorRngSeed);
                        ChallengeFloor.History.Clear();
                        Roguelike.SeenMineMaps.Clear();
                    }
                    break;
                case "stonechance":
                    if (args.Length == 1)
                        ModEntry.ModMonitor.Log($"Stone chance is currently: {ForcedStoneChance}", LogLevel.Info);
                    else
                        ForcedStoneChance = float.Parse(args[1]);
                    break;
                case "gemchance":
                    if (args.Length == 1)
                        ModEntry.ModMonitor.Log($"Gem chance is currently: {ForcedGemChance}", LogLevel.Info);
                    else
                        ForcedGemChance = float.Parse(args[1]);
                    break;
                case "monsterchance":
                    if (args.Length == 1)
                        ModEntry.ModMonitor.Log($"Monster chance is currently: {ForcedMonsterChance}", LogLevel.Info);
                    else
                        ForcedMonsterChance = float.Parse(args[1]);
                    break;
                case "darkchance":
                    if (args.Length == 1)
                        ModEntry.ModMonitor.Log($"Dark chance is currently: {ForcedDarkChance}", LogLevel.Info);
                    else
                        ForcedDarkChance = float.Parse(args[1]);
                    break;
                case "hardmode":
                    if (args.Length == 1)
                        ModEntry.ModMonitor.Log($"Hard mode is currently: {Roguelike.HardMode}", LogLevel.Info);
                    else
                    {
                        Roguelike.HardMode = bool.Parse(args[1]);
                        ModEntry.ActiveStats.HardMode = bool.Parse(args[1]);
                    }
                    break;
                case "printstats":
                    ModEntry.ModMonitor.Log(JsonSerializer.Serialize(ModEntry.ActiveStats), LogLevel.Info);
                    break;
                case "forceboss":
                    if (args.Length == 1)
                    {
                        ModEntry.ModMonitor.Log($"-1 = disabled", LogLevel.Info);
                        var bosses = BossManager.GetFlattenedBosses();
                        for (int i = 0; i < bosses.Count; i++)
                            ModEntry.ModMonitor.Log($"{i} = {bosses[i]}", LogLevel.Info);
                    }
                    else
                        ForcedBossIndex = int.Parse(args[1]);
                    break;
                case "forcechallenge":
                    if (args.Length == 1)
                    {
                        ModEntry.ModMonitor.Log($"-1 = disabled", LogLevel.Info);
                        var values = Enum.GetValues<ChallengeFloor.ChallengeType>();
                        for (int i = 0; i < values.Length; i++)
                            ModEntry.ModMonitor.Log($"{i} = {values[i]}", LogLevel.Info);
                    }
                    else
                        ForcedChallengeIndex = int.Parse(args[1]);
                    break;
                case "forceforge":
                    if (args.Length == 1)
                        ModEntry.ModMonitor.Log($"Forge is currently: {ForcedForgeFloor}", LogLevel.Info);
                    else
                        ForcedForgeFloor = bool.Parse(args[1]);
                    break;
                case "forcechest":
                    if (args.Length == 1)
                        ModEntry.ModMonitor.Log($"Force chest is currently: {ForcedChestFloor}", LogLevel.Info);
                    else
                        ForcedChestFloor = bool.Parse(args[1]);
                    break;
                case "forcefortune":
                    if (args.Length == 1)
                        ModEntry.ModMonitor.Log($"Forced fortune is currently: {ForcedFortuneTeller}", LogLevel.Info);
                    else
                        ForcedFortuneTeller = bool.Parse(args[1]);
                    break;
                case "forcegil":
                    if (args.Length == 1)
                        ModEntry.ModMonitor.Log($"Forced gil is currently: {ForcedGil}", LogLevel.Info);
                    else
                        ForcedGil = bool.Parse(args[1]);
                    break;
                case "difficulty":
                    if (args.Length == 1)
                    {
                        MineShaft curmine = (MineShaft)Game1.player.currentLocation;
                        int level = Roguelike.GetLevelFromMineshaft(curmine);
                        float difficulty = BossFloor.GetLevelDifficulty(level);
                        bool postloop = level > Constants.ScalingOrder[^1];
                        ModEntry.ModMonitor.Log($"level: {level} difficulty: {difficulty} postloop: {postloop}");
                    }
                    else
                        ForcedDifficulty = float.Parse(args[1]);
                    break;
                case "level":
                    if (args.Length == 1)
                        ModEntry.ModMonitor.Log($"Level is currently: {Roguelike.CurrentLevel}", LogLevel.Info);
                    else
                        Roguelike.CurrentLevel = int.Parse(args[1]);
                    break;
                case "stamina":
                    if (args.Length == 1)
                        ModEntry.ModMonitor.Log($"Stamina is currently: {Game1.player.Stamina}", LogLevel.Info);
                    else
                        Game1.player.Stamina = int.Parse(args[1]);
                    break;
                case "genmines":
                    if (args.Length == 1)
                        ModEntry.ModMonitor.Log($"Mines in memory: active={MineShaft.activeMines.Count}", LogLevel.Info);
                    else
                    {
                        int highestLevel = Roguelike.GetHighestMineShaftLevel();
                        if (highestLevel == 0)
                        {
                            ModEntry.ModMonitor.Log($"Enter the first level before using this command.", LogLevel.Error);
                            return;
                        }

                        int toGen = int.Parse(args[1]);
                        for (int i = highestLevel + 1; i <= highestLevel + toGen; i++)
                            MineShaft.GetMine($"UndergroundMine1/{i}");
                    }
                    break;
                case "enter":
                    {
                        // Drives the same path the lobby ladder does, so floor entry
                        // can be tested without the GUI.
                        int target = args.Length > 1 ? int.Parse(args[1]) : Roguelike.CurrentLevel + 1;
                        Roguelike.CurrentLevel = target - 1;
                        Game1.enterMine(target);
                        ModEntry.ModMonitor.Log($"entering mine level {target}", LogLevel.Info);
                    }
                    break;
                // Reports which game is running and how many patches are live, so a
                // mode switch can be checked without reading the title screen.
                case "mode":
                    {
                        if (args.Length > 1)
                        {
                            bool wanted = args[1] == "on";
                            if (wanted)
                                RoguelikeEntry.Begin();
                            else
                                RoguelikeEntry.Leave();

                            ModEntry.ModMonitor.Log($"requested mode {(wanted ? "on" : "off")}", LogLevel.Info);
                            break;
                        }

                        var patched = Harmony.GetAllPatchedMethods().ToList();
                        int core = 0, scoped = 0;
                        foreach (var method in patched)
                        {
                            var info = Harmony.GetPatchInfo(method);
                            if (info is null)
                                continue;
                            foreach (var owner in info.Owners)
                            {
                                if (owner == Patch.AlwaysActiveId) core++;
                                else if (owner == Patch.ModeScopedId) scoped++;
                            }
                        }

                        ModEntry.ModMonitor.Log(
                            $"roguelike mode: {(RoguelikeMode.IsActive ? "ON" : "off")}; " +
                            $"patched methods: {core} always-active, {scoped} mode-scoped",
                            LogLevel.Info);
                        ModEntry.ModMonitor.Log(
                            $"location={Game1.currentLocation?.Name ?? "none"} " +
                            $"mineMap={(Game1.getLocationFromName("Mine")?.mapPath.Value ?? "n/a")}",
                            LogLevel.Info);
                    }
                    break;

                // Writes a currently loaded texture out to PNG. Run with the mode
                // off to capture vanilla art, so the mod's own sheets can be diffed
                // against it and reduced to the regions that actually differ.
                case "dumpsheet":
                    {
                        string assetName = args.Length > 1 ? args[1] : "LooseSprites/Cursors";
                        Texture2D texture = Game1.content.Load<Texture2D>(assetName);
                        string outPath = Path.Combine(
                            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                            "dump_" + (RoguelikeMode.IsActive ? "on_" : "off_")
                                + assetName.Replace('/', '_').Replace('\\', '_') + ".png");

                        using (FileStream stream = File.Create(outPath))
                            texture.SaveAsPng(stream, texture.Width, texture.Height);

                        ModEntry.ModMonitor.Log(
                            $"dumped {assetName} ({texture.Width}x{texture.Height}) -> {outPath}",
                            LogLevel.Info);
                    }
                    break;

                case "display":
                    {
                        ModEntry.ModMonitor.Log(
                            $"zoomLevel={Game1.options.zoomLevel} uiScale={Game1.options.uiScale} " +
                            $"baseZoom={Game1.options.desiredBaseZoomLevel} " +
                            $"viewport={Game1.viewport.Width}x{Game1.viewport.Height} " +
                            $"uiViewport={Game1.uiViewport.Width}x{Game1.uiViewport.Height}",
                            LogLevel.Info);

                        foreach (string name in new[] { "draw", "drawMoneyBox", "updatePosition" })
                        {
                            var method = AccessTools.Method(typeof(StardewValley.Menus.DayTimeMoneyBox), name);
                            var info = method is null ? null : Harmony.GetPatchInfo(method);
                            ModEntry.ModMonitor.Log(
                                $"DayTimeMoneyBox.{name}: method={(method is not null)} " +
                                $"prefixes={info?.Prefixes.Count ?? 0} postfixes={info?.Postfixes.Count ?? 0}",
                                LogLevel.Info);
                        }
                    }
                    break;

                // The mod compares ParentSheetIndex to int literals in six places.
                // In 1.6 that property is the sprite index from Data/Objects, not the
                // item id, so this reports whether they still coincide.
                case "sheetids":
                    {
                        int mismatched = 0;
                        foreach (string id in new[] { "384", "803", "239", "265", "62", "599", "848", "668" })
                        {
                            SObject obj = new(id, 1);
                            bool ok = obj.ParentSheetIndex.ToString() == id;
                            if (!ok) mismatched++;
                            ModEntry.ModMonitor.Log(
                                $"  id={id} name={obj.Name} ParentSheetIndex={obj.ParentSheetIndex} " +
                                (ok ? "matches" : "MISMATCH"),
                                ok ? LogLevel.Info : LogLevel.Error);
                        }
                        ModEntry.ModMonitor.Log($"sheet id check: {mismatched} mismatch(es)",
                            mismatched > 0 ? LogLevel.Error : LogLevel.Info);
                    }
                    break;

                // Kills the player the way a monster would, so the death path can be
                // watched end to end: the mod is supposed to catch Game1.killScreen
                // before vanilla's pass-out sequence gets going.
                case "die":
                    {
                        Game1.player.health = 0;
                        ModEntry.ModMonitor.Log(
                            $"set health to 0 at {Game1.currentLocation?.Name ?? "none"}; " +
                            $"killScreen={Game1.killScreen} invincible={ModEntry.Invincible}",
                            LogLevel.Info);
                    }
                    break;

                case "deathstate":
                    ModEntry.ModMonitor.Log(
                        $"location={Game1.currentLocation?.Name ?? "none"} " +
                        $"health={Game1.player.health}/{Game1.player.maxHealth} " +
                        $"killScreen={Game1.killScreen} eventUp={Game1.eventUp} " +
                        $"mode={RoguelikeMode.IsActive} level={Roguelike.CurrentLevel}",
                        LogLevel.Info);
                    break;

                case "load":
                    {
                        // Same path LoadGameMenuPatch takes when the Load menu opens,
                        // so the harness can get into the save without the GUI.
                        string savePath = Path.Combine(
                            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                            $@"assets/Saves/{Constants.SaveFile}/SaveGameInfo");
                        SaveGame.Load(savePath);
                        Game1.exitActiveMenu();
                        ModEntry.ModMonitor.Log($"loading save from {savePath}", LogLevel.Info);
                    }
                    break;
                case "autofight":
                    DebugAutoFight.Start(args.Length > 1 ? int.Parse(args[1]) : 30);
                    break;
                case "stopfight":
                    DebugAutoFight.Stop("asked to stop");
                    break;
                case "where":
                    {
                        GameLocation here = Game1.player.currentLocation;
                        string ladder = here is MineShaft ms ? ms.tileBeneathLadder.ToString() : "n/a";
                        bool onMap = here?.isTileOnMap(Game1.player.Tile) ?? false;
                        ModEntry.ModMonitor.Log(
                            $"location={here?.Name} tile={Game1.player.Tile} onMap={onMap} " +
                            $"tileBeneathLadder={ladder} map={(here is MineShaft m2 ? m2.get_MineShaftLoadedMap().Value : "n/a")}",
                            LogLevel.Info);
                    }
                    break;
                case "buffs":
                    {
                        var applied = Game1.player.buffs.AppliedBuffs;
                        ModEntry.ModMonitor.Log($"applied buffs ({applied.Count}):", LogLevel.Info);
                        foreach (var (id, buff) in applied)
                        {
                            ModEntry.ModMonitor.Log(
                                $"  {id}  visible={buff.visible} ms={buff.millisecondsDuration} " +
                                $"luck={buff.effects.LuckLevel.Value} speed={buff.effects.Speed.Value} " +
                                $"def={buff.effects.Defense.Value} imm={buff.effects.Immunity.Value}",
                                LogLevel.Info);
                        }
                        ModEntry.ModMonitor.Log(
                            $"aggregate: luck={Game1.player.buffs.LuckLevel} speed={Game1.player.buffs.Speed} " +
                            $"def={Game1.player.buffs.Defense} imm={Game1.player.buffs.Immunity}",
                            LogLevel.Info);
                        ModEntry.ModMonitor.Log(
                            $"perks={string.Join(",", Perks.GetActivePerks())} curses={Game1.player.get_FarmerCurses().Count}",
                            LogLevel.Info);
                    }
                    break;
                case "gamba":
                    Game1.activeClickableMenu = new GambaMenu();
                    break;
                case "skills":
                    ModEntry.ModMonitor.Log($"luck: base={Game1.player.luckLevel.Value} added={Game1.player.buffs.LuckLevel} daily={Game1.player.DailyLuck} average={Game1.player.team.AverageLuckLevel()}", LogLevel.Info);
                    ModEntry.ModMonitor.Log($"speed: base={Game1.player.speed} added={Game1.player.addedSpeed} result={Game1.player.getMovementSpeed()}", LogLevel.Info);
                    break;
                case "food":
                    ModEntry.ModMonitor.Log($"food: {(Game1.player.buffs.AppliedBuffs.TryGetValue("food", out Buff? foodBuff) ? foodBuff.displayName : "none")}", LogLevel.Info);
                    ModEntry.ModMonitor.Log($"drink: {(Game1.player.buffs.AppliedBuffs.TryGetValue("drink", out Buff? drinkBuff) ? drinkBuff.displayName : "none")}", LogLevel.Info);
                    break;
                case "boulder":
                    MineShaft mine = (MineShaft)Game1.player.currentLocation;
                    int whichClump = (Game1.random.NextDouble() < 0.5) ? 752 : 754;
                    if (mine.getMineArea() == 40)
                    {
                        if (mine.GetAdditionalDifficulty() > 0)
                        {
                            whichClump = 600;
                            if (Roguelike.FloorRng.NextDouble() < 0.1)
                                whichClump = 602;
                        }
                        else
                            whichClump = ((Roguelike.FloorRng.NextDouble() < 0.5) ? 756 : 758);
                    }
                    mine.resourceClumps.Add(new ResourceClump(whichClump, 2, 2, Game1.currentCursorTile));
                    break;
                case "exportmap":
                    ExportMap();
                    break;
                case "hatboard":
                    Game1.activeClickableMenu = new HatBoard();
                    break;
                default:
                    ModEntry.ModMonitor.Log("Invalid command.", LogLevel.Error);
                    break;
            };
        }

        public static void PrintHelp()
        {
            StringBuilder help = new();

            help.AppendLine("Debug Commands:");
            help.AppendLine("perk [int:which] : gives yourself a specific perk");
            help.AppendLine("perkpick : opens up the perk picking menu");
            help.AppendLine("perks : gives yourself all perks");
            help.AppendLine("clearperks : clears all perks");
            help.AppendLine("curse [int:which] : gives yourself a specific curse");
            help.AppendLine("curses : gives yourself all curses");
            help.AppendLine("clearcurses : clears all curses");
            help.AppendLine("ladder <int:tile x> <int:tile y> : forces a ladder to spawn");
            help.AppendLine("reset : force resets the run");
            help.AppendLine("spectate : enter spectator mode");
            help.AppendLine("unspectate : exit spectator mode");
            help.AppendLine("seed [int:seed] : sets/views the seed for floor generation");
            help.AppendLine("stonechance <float:chance> : sets the chance for stone spawns");
            help.AppendLine("gemchance <float:chance> : sets the chance for gem spawns");
            help.AppendLine("monsterchance <float:chance> : sets the chance for monster spawns");
            help.AppendLine("darkchance <float:chance> : sets the chance for dark floors on hard mode");
            help.AppendLine("hardmode [bool:status] : enable or disable hard mode");
            help.AppendLine("printstats : prints the stats payload");
            help.AppendLine("forceboss [int:which] : forces boss floors to appear when possible");
            help.AppendLine("forcechallenge [int:which] : forces challenge floors to appear when possible");
            help.AppendLine("forceforge [bool:status] : forces forge floors to appear when possible");
            help.AppendLine("forcechest [bool:status] : forces chest floors to appear when possible");
            help.AppendLine("forcefortune [bool:status] : force the fortune teller to spawn");
            help.AppendLine("forcegil [bool:status] : force gil to spawn");
            help.AppendLine("difficulty <float:new> : forces the difficulty to be at a certain value");
            help.AppendLine("level <int:new> : sets the current level");
            help.AppendLine("stamina [int:new] : sets the player's stamina");
            help.AppendLine("genmines <int:amount> : generates amount mines and stores in memory");
            help.AppendLine("gamba : opens the gamba wheel menu");
            help.AppendLine("skills : displays the player's luck and speed skills");
            help.AppendLine("food : displays active food and drink buffs");
            help.AppendLine("boulder : spawns a resource clump (boulder/log) at cursor tile");
            help.AppendLine("exportmap : exports the current location to a tmx file");
            help.AppendLine("hatboard : opens the hat board menu");

            ModEntry.ModMonitor.Log(help.ToString(), LogLevel.Info);
        }

        public static void ButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (e.Button == SButton.RightAlt)
                ForceLadder(new string[] { "", $"{Game1.currentCursorTile.X}", $"{Game1.currentCursorTile.Y}" });
            else if (e.Button == SButton.RightControl)
            {
                MineShaft mine = (MineShaft)Game1.player.currentLocation;
                mine.SpawnLocalChest(new(Game1.currentCursorTile.X, Game1.currentCursorTile.Y));
            }
            else if (e.Button == SButton.L)
            {
                RoguelikeUtility.DoAttackCue(Game1.player.currentLocation, 60);
            }
        }

        public static void ForceLadder(string[] args)
        {
            if (args.Length != 3)
            {
                ModEntry.ModMonitor.Log("Invalid usage: ladder x y", LogLevel.Error);
                return;
            }
            else if (Game1.player.currentLocation is not MineShaft)
            {
                ModEntry.ModMonitor.Log("You are not in a mineshaft.", LogLevel.Error);
                return;
            }

            MineShaft mine = (MineShaft)Game1.player.currentLocation;

            int x = int.Parse(args[1]);
            int y = int.Parse(args[2]);

            NetVector2Dictionary<bool, NetBool> createLadderEvent = (NetVector2Dictionary<bool, NetBool>)mine.GetType().GetField("createLadderAtEvent", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(mine)!;
            createLadderEvent[new Vector2(x, y)] = true;
        }

        public static void ExportMap()
        {
            xTile.Map map = Game1.player.currentLocation.Map;

            TMXTile.TMXFormat Format = new(Game1.tileSize / Game1.pixelZoom, Game1.tileSize / Game1.pixelZoom, Game1.pixelZoom, Game1.pixelZoom);
            TMXTile.TMXMap tmxMap = Format.Store(map);

            foreach (var objectGroup in tmxMap.Objectgroups)
            {
                for (int i = 0; i < objectGroup.Objects.Count; i++)
                {
                    var tmxObject = objectGroup.Objects[i];
                    tmxObject.Properties = tmxObject.Properties.Skip(2).ToArray();
                    if (tmxObject.Properties.Length == 0)
                        objectGroup.Objects[i] = null;
                }
            }

            foreach (var tileset in tmxMap.Tilesets)
                tileset.Image.Source = Path.GetFileName(tileset.Image.Source);

            var parser = new TMXTile.TMXParser();
            parser.Export(tmxMap, "_map.tmx", TMXTile.DataEncodingType.XML);

            Console.WriteLine("Map successfully exported.");
        }
    }
}

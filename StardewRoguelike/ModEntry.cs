using Microsoft.Xna.Framework;
using StardewHitboxes;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Toolkit.Framework.Clients.WebApi;
using StardewRoguelike.Bosses;
using StardewRoguelike.Netcode;
using StardewRoguelike.UI;
using StardewValley;
using StardewModdingAPI.Utilities;
using StardewRoguelike.Mode;
using StardewRoguelike.Run;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;

namespace StardewRoguelike
{
    public class ModEntry : Mod
    {
        public static readonly string WebsiteBaseUrl = "https://roguelike.tyler.solutions";

        /// <summary>
        /// This mod's unique ID, used to prefix the string IDs that 1.6
        /// requires for things like buffs.
        /// </summary>
        public const string ModId = "tylergibbs2.StardewRoguelike";

        public static ModEntry Instance { get; private set; } = null!;

        public static IModEvents Events { get; private set; } = null!;

        public static IDataHelper DataHelper { get; private set; } = null!;

        public static IMultiplayerHelper MultiplayerHelper { get; private set; } = null!;

        public static IGameContentHelper GameContentHelper { get; private set; } = null!;

        public static IReflectionHelper ReflectionHelper { get; private set; } = null!;

        public static IModRegistry ModRegistry { get; private set; } = null!;

        public static IMonitor ModMonitor { get; private set; } = null!;

        /// <summary>
        /// Tracks the statistics of the game.
        /// </summary>
        public static readonly Stats ActiveStats = new();

        private static readonly PerScreen<bool> ScreenInvincible = new(() => false);

        /// <summary>Whether the player on this screen is in the death grace period.</summary>
        public static bool Invincible
        {
            get => ScreenInvincible.Value;
            set => ScreenInvincible.Value = value;
        }

        public static ModConfig Config { get; private set; } = null!;

        public static string? CurrentVersion { get; private set; } = null;
        public static string? NewUpdateVersion { get; private set; } = null;

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            Instance = this;

            I18n.Init(helper.Translation);
            Config = helper.ReadConfig<ModConfig>();
            // Only the patches that let the player pick a mode. The rest are
            // applied when they actually choose the roguelike, so a vanilla save
            // runs on a stock game.
            Patch.PatchAlwaysActive();

            RoguelikeMode.Register(new HarmonyEffect());
            RoguelikeMode.Register(new EventEffect());
            RoguelikeMode.Register(new AssetEffect());
            RoguelikeMode.Register(new StateEffect());

            // Assign references to various helpers to
            // static fields.
            Events = helper.Events;
            DataHelper = helper.Data;
            MultiplayerHelper = helper.Multiplayer;
            GameContentHelper = helper.GameContent;
            ReflectionHelper = helper.Reflection;
            ModRegistry = helper.ModRegistry;
            ModMonitor = Monitor;

            // Initialize all event handlers
            //
            // Custom audio waits for GameLaunched rather than loading here. The game
            // builds its sound bank from Initialize as a threaded task, so at Entry
            // it may not exist yet or may still be replaced afterwards - and cues
            // added to a bank that is then thrown away go quietly, with no error and
            // no music.
            helper.Events.GameLoop.GameLaunched += (_, _) => ContentManager.SetupAudio();

            // And again when a save loads, in case the bank that existed at launch
            // was not the one the game ended up using. SetupAudio skips cues that are
            // already present, so running it twice costs nothing.
            helper.Events.GameLoop.SaveLoaded += (_, _) => ContentManager.SetupAudio();
            helper.Events.GameLoop.GameLaunched += SetupGMCM;
            helper.Events.GameLoop.GameLaunched += CheckForUpdate;
            // Carries out a pending mode switch from the update loop, where none of
            // our own patches are on the stack to be pulled out from under.
            helper.Events.GameLoop.UpdateTicked += RoguelikeMode.UpdateTicked;

            // Runs after the switch above, so it only loads once the mode is on.
            helper.Events.GameLoop.UpdateTicked += RoguelikeEntry.UpdateTicked;

            // Leaving to the title hands the game back, whether the run ended or the
            // player quit out of it. Registered here rather than in the mode's own
            // handlers, which are gone by the time the switch finishes.
            helper.Events.GameLoop.ReturnedToTitle += (_, _) => RoguelikeEntry.Leave();

            // Stays subscribed in both modes and declines to replace anything while
            // the roguelike is off.
            // The way into a run, drawn over the title screen instead of patching it.
            helper.Events.Display.RenderedActiveMenu += TitleRoguelikeEntry.RenderedActiveMenu;
            helper.Events.Input.ButtonPressed += TitleRoguelikeEntry.ButtonPressed;

            helper.Events.Content.AssetRequested += ContentManager.AssetRequested;

            // How a farmhand learns the host is running a roguelike, which has to
            // work before the client has entered the mode.
            helper.Events.Multiplayer.ModMessageReceived += ModMessageReceived;
            helper.Events.Multiplayer.PeerContextReceived += CoopHandshake.PeerContextReceived;

            var assemblyConfiguration = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyConfigurationAttribute>();
            if (assemblyConfiguration?.Configuration == "Debug")
            {
                helper.ConsoleCommands.Add("rdebug", "Debugging command suite for the Roguelike.", DebugCommands.Parse);
                helper.Events.Input.ButtonPressed += DebugCommands.ButtonPressed;
            }
        }

        public void CheckForUpdate(object? sender, GameLaunchedEventArgs e)
        {
            object? metadata = Helper.ModRegistry.Get(Helper.ModRegistry.ModID);
            if (metadata is null)
                return;

            ModEntryModel? updateResult = (ModEntryModel?)metadata.GetType().GetProperty("UpdateCheckData", BindingFlags.Instance | BindingFlags.Public)!.GetValue(metadata);

            CurrentVersion = ((IModInfo)metadata).Manifest.Version.ToString();

            if (updateResult is null || updateResult.SuggestedUpdate is null)
                return;

            NewUpdateVersion = updateResult.SuggestedUpdate.Version.ToString();
        }

        public void RenderHud(object? sender, RenderedHudEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            if (NewUpdateVersion is not null)
            {
                string newVersionText = I18n.UI_NewUpdate(version: NewUpdateVersion);
                Vector2 newVersionTextSize = Game1.smallFont.MeasureString(newVersionText);

                Point drawPos = new((int)(Game1.uiViewport.Width - newVersionTextSize.X - 40), 140);

                IClickableMenu.drawTextureBox(
                    e.SpriteBatch,
                    drawPos.X - 15,
                    drawPos.Y - 12,
                    (int)newVersionTextSize.X + 33,
                    (int)newVersionTextSize.Y + 25,
                    Color.White
                );

                Utility.drawTextWithShadow(
                    e.SpriteBatch,
                    newVersionText,
                    Game1.smallFont,
                    new Vector2(drawPos.X, drawPos.Y + 3),
                    Color.Black
                );
            }
            if (CurrentVersion is not null)
            {
                Vector2 versionTextSize = Game1.tinyFont.MeasureString(CurrentVersion);

                Vector2 drawPos = new((Game1.uiViewport.Width - versionTextSize.X - 100), 80);

                e.SpriteBatch.DrawString(
                    Game1.tinyFont,
                    CurrentVersion,
                    drawPos,
                    Color.White
                );
            }
        }

        /// <summary>
        /// Event handler for when the Game1 activeClickableMenu changes.
        /// This persists the player's game options when they close the menu.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="MenuChangedEventArgs"/> instance containing the event data.</param>
        public void MenuChanged(object? sender, MenuChangedEventArgs e)
        {
            if (e.OldMenu is GameMenu && e.NewMenu is null)
                Helper.Data.WriteGlobalData("RoguelikeGameOptions", Game1.options);
        }

        private void SetupGMCM(object? sender, GameLaunchedEventArgs e)
        {
            IGenericModConfigMenuApi? configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            configMenu.Register(
                mod: ModManifest,
                reset: () => Config = new ModConfig(),
                save: () => Helper.WriteConfig(Config)
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Config_SkipCharacter_Name(),
                tooltip: () => I18n.Config_SkipCharacter_Tooltip(),
                getValue: () => Config.SkipCharacterCreation,
                setValue: value => Config.SkipCharacterCreation = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Config_BossSplits_Name(),
                tooltip: () => I18n.Config_BossSplits_Tooltip(),
                getValue: () => Config.DisableBossSplits,
                setValue: value => Config.DisableBossSplits = value
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.Config_AutomaticallyFaceMouse_Name(),
                tooltip: () => I18n.Config_AutomaticallyFaceMouse_Tooltip(),
                getValue: () => Config.AutomaticallyFaceMouse,
                setValue: value => Config.AutomaticallyFaceMouse = value
            );
        }

        /// <summary>
        /// Event handler for messages between multiplayer mod clients.
        /// This handles player respawning and "game over"ing runs.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="ModMessageReceivedEventArgs"/> instance containing the event data.</param>
        public void ModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
        {
            // Ignore all messages that aren't from this mod.
            if (e.FromModID != ModManifest.UniqueID)
                return;

            if (e.Type == CoopHandshake.MessageType)
            {
                CoopHandshake.Receive(e);
            }
            else if (e.Type == "RespawnMessage") // Perform a respawn on this client
            {
                RespawnMessage message = e.ReadAs<RespawnMessage>();
                ActiveStats.EndTime = null;

                SpectatorMode.ExitSpectatorMode(message.RespawnLevel);
                Invincible = false;
                Game1.player.health = Game1.player.maxHealth;
            }
            else if (e.Type == "BossDeath")
            {
                BossDeathMessage message = e.ReadAs<BossDeathMessage>();
                Game1.onScreenMenus.Add(
                    new BossKillAnnounceMenu(message.BossName, message.KillSeconds)
                );
            }
            else if (e.Type == "AttackIndicator")
            {
                AttackIndicatorMessage message = e.ReadAs<AttackIndicatorMessage>();
                message.Trigger();
            }
            else if (e.Type == RunRoute.MessageType)
            {
                RunRoute.Receive(e.ReadAs<RunRoute.ChoicePayload>());
            }
            else if (e.Type == "GameOver") // Signal that the game is over
                Roguelike.GameOver();
        }
    }
}

using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace LiveProgressBar
{
    public class ModEntry : Mod
    {
        internal ModConfig? Config { get; private set; } = null;

        internal bool menuVisible { get; private set; } = true;
        private float lastProgress;

        private ProgressHUD progressHUD { get; set; }

        public override void Entry(IModHelper helper)
        {
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;

            helper.ConsoleCommands.Add("progress", "Sets a fake progress percentage for testing.", SetProgressCmd);
        }


        private void SetProgressCmd(string command, string[] args)
        {
            progressHUD.SetProgress(float.Parse(args[0]));
        }


        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            Config = Helper.ReadConfig<ModConfig>();
            lastProgress = 0f;
            progressHUD = new ProgressHUD(lastProgress);
            progressHUD.SetVisible(menuVisible);

            Game1.onScreenMenus.Add(progressHUD);
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (Config is not null && Config.ToggleKey.JustPressed())
            {
                menuVisible = !menuVisible;
                progressHUD.SetVisible(menuVisible);
            }
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            if (!menuVisible)
                return;

            float latestProgress = Utility.percentGameComplete();
            if (latestProgress == lastProgress)
                return;

            Monitor.Log($"Progress Changed: {latestProgress:P2}.", LogLevel.Info);
            lastProgress = latestProgress;

            progressHUD.SetProgress(latestProgress);
        }
    }
}

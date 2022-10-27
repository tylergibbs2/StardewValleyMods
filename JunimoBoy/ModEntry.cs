using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Minigames;
using System;

namespace JunimoBoy
{
    internal class ModEntry : Mod
    {
        public static Texture2D ItemTextures { get; private set; }

        public override void Entry(IModHelper helper)
        {
            I18n.Init(helper.Translation);

            ItemTextures = helper.ModContent.Load<Texture2D>("assets/TileSheets/items.png");

            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.GameLoop.GameLaunched += (s, e) => InitSpaceCore(helper.ModRegistry);

            helper.ConsoleCommands.Add("jb_giveitem", "Gives you a Junimo Boy", GiveJunimoBoy);
        }

        private void InitSpaceCore(IModRegistry modRegistry)
        {
            SpaceCore.IApi api = modRegistry.GetApi<SpaceCore.IApi>("spacechase0.SpaceCore");
            if (api is null)
                throw new Exception("missing spacecore dep somehow");

            api.RegisterSerializerType(typeof(JunimoBoy));
        }

        private void OnQuestionAnswer(Farmer who, string answer)
        {
            switch (answer)
            {
                case "Kart":
                    Game1.currentMinigame = new MineCart(0, 3);
                    break;
                case "JOTPK":
                    Game1.currentMinigame = new AbigailGame();
                    break;
            }
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady || !e.Button.IsActionButton() || Game1.player.CurrentItem is not JunimoBoy junimoBoy)
                return;

            GameLocation location = Game1.player.currentLocation;

            var responses = new Response[3]
            {
                new Response("Kart", "Junimo Kart"),
                new Response("JOTPK", "Journey of the Prairie King"),
                new Response("Cancel", "Turn off")
            };

            location.createQuestionDialogue(I18n.JunimoBoy_Startup(), responses, OnQuestionAnswer);
        }

        private void GiveJunimoBoy(string command, string[] args)
        {
            if (!Context.IsWorldReady)
                return;

            Game1.player.addItemByMenuIfNecessary(new JunimoBoy());
        }
    }
}

using HarmonyLib;
using StardewValley;
using StardewValley.Menus;

namespace StardewRoguelike.Patches
{
    [HarmonyPatch(typeof(ChatBox), "runCommand")]
    internal class InterceptCommand
    {
        public static bool Prefix(ChatBox __instance, string commandText)
        {
            // 1.6 removed ChatBox.showHelp and moved "help" into the ChatCommands
            // registry, so the roguelike's own help listing is handled here instead
            // of in a second patch.
            if (ArgUtility.SplitBySpaceAndGet(commandText, 0) == "help")
            {
                ShowHelp(__instance);
                return false;
            }

            return CommandHandler.Handle(commandText);
        }

        private static void ShowHelp(ChatBox chatBox)
        {
            chatBox.addInfoMessage(Game1.content.LoadString("Strings\\UI:Chat_Help"));
            chatBox.addInfoMessage("stats: shows the current run stats");
            chatBox.addInfoMessage("character: opens the character customization menu");
            chatBox.addInfoMessage("reset: resets the run");
            chatBox.addInfoMessage("stuck: helps ensure progression");

            if (Game1.IsServer)
            {
                chatBox.addInfoMessage(Game1.content.LoadString("Strings\\UI:Chat_HelpKick", "kick"));
                chatBox.addInfoMessage(Game1.content.LoadString("Strings\\UI:Chat_HelpBan", "ban"));
                chatBox.addInfoMessage(Game1.content.LoadString("Strings\\UI:Chat_HelpUnban", "unban"));
            }
        }
    }
}

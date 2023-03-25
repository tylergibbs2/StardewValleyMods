using HarmonyLib;
using StardewValley.Menus;

namespace Circuit.Patches
{
    [HarmonyPatch(typeof(ChatBox), "runCommand")]
    internal class ChatBoxRunCommandPatch
    {
        public static bool Prefix(string command)
        {
            return !ChatCommands.HandleCommand(command);
        }
    }
}
